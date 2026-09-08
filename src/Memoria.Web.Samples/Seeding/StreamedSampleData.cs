using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;
using Memoria.Results;
using Memoria.Web.Samples.Streamed.Aggregates;
using Memoria.Web.Samples.Streamed.Projections;
using Memoria.Web.Samples.Streamed.Streams;
using static Memoria.Web.Samples.Seeding.SampleVocabulary;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Writes customers, their orders, and the models folded from them.
/// </summary>
/// <remarks>
/// <para>
/// Every order is produced by driving the <see cref="Order"/> aggregate through its own methods, so
/// the log holds sequences the model would actually have allowed. Nothing here appends an event the
/// domain would have refused.
/// </para>
/// <para>
/// Snapshots are deliberately left in three states — up to date, behind, and not written at all —
/// because a store where everything is current has nothing to demonstrate. A model is left behind by
/// snapshotting it, then appending more events to its stream without snapshotting again; that is
/// exactly what happens in a real application between a write and the next read that refreshes.
/// </para>
/// </remarks>
public static class StreamedSampleData
{
    public static async Task Add(
        IDomainService store,
        Random random,
        TimeProvider time,
        SeedReport report,
        CancellationToken cancellationToken = default)
    {
        var customers = random.Next(2, 5);

        for (var customer = 0; customer < customers; customer++)
        {
            await AddCustomer(store, random, time, report, cancellationToken);
        }
    }

    private static async Task AddCustomer(
        IDomainService store,
        Random random,
        TimeProvider time,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        var customerId = Id(random, "c");
        var stream = new CustomerStreamId(customerId);
        var orders = random.Next(1, 5);

        // Which order the customer's two whole-stream models are refreshed after. Land on the last
        // one and they end up current; land earlier and every order after it leaves them behind.
        var refreshAccountAfter = random.Next(orders);
        var refreshHistoryAfter = random.Next(orders);

        for (var order = 0; order < orders; order++)
        {
            await AddOrder(store, stream, customerId, random, time, report, cancellationToken);

            if (order == refreshAccountAfter)
            {
                await store.UpdateAggregate(stream, new CustomerAccountId(customerId), cancellationToken);
            }

            if (order == refreshHistoryAfter)
            {
                await store.UpdateProjection(stream, new CustomerOrderHistoryId(customerId), cancellationToken);
            }
        }

        report.Add(new SeededModel("streamed", "aggregate", nameof(CustomerAccount),
            nameof(CustomerAccountId), customerId,
            () => MeasureAggregate(store, stream, new CustomerAccountId(customerId))));

        report.Add(new SeededModel("streamed", "projection", nameof(CustomerOrderHistory),
            nameof(CustomerOrderHistoryId), customerId,
            () => MeasureProjection(store, stream, new CustomerOrderHistoryId(customerId))));
    }

    private static async Task AddOrder(
        IDomainService store,
        CustomerStreamId stream,
        string customerId,
        Random random,
        TimeProvider time,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        var orderId = Id(random, "o");
        var steps = Script(orderId, customerId, random, time);

        // How much of the order's life the snapshot is written at. Half the time that is all of it;
        // otherwise the last event or three are appended afterwards and left for an update to fold.
        var snapshotAt = Chance(random, 50)
            ? steps.Count
            : Math.Max(1, steps.Count - random.Next(1, 4));

        var head = new Order();
        Drive(head, steps.Take(snapshotAt));
        var headEvents = head.UncommittedEvents.ToList();

        var sequence = await LatestSequence(store, stream, cancellationToken);
        Check(await store.SaveAggregate(stream, new OrderId(orderId), head, sequence, cancellationToken));
        report.Appended(headEvents.Count);

        // The summary is refreshed before or after the rest of the order is appended, which is the
        // only difference between a read model that is current and one that is waiting.
        var refreshSummaryEarly = Chance(random, 50);

        if (refreshSummaryEarly)
        {
            await store.UpdateProjection(stream, new OrderSummaryId(orderId), cancellationToken);
        }

        if (snapshotAt < steps.Count)
        {
            // A second instance folded from what was just written, so the remaining steps decide on
            // the same state the first instance reached without re-staging what it already appended.
            var tail = new Order();
            tail.Apply(headEvents);
            Drive(tail, steps.Skip(snapshotAt));

            var tailEvents = tail.UncommittedEvents.ToArray();
            sequence = await LatestSequence(store, stream, cancellationToken);
            Check(await store.SaveEvents(stream, tailEvents, sequence, cancellationToken));
            report.Appended(tailEvents.Length);
        }

        if (!refreshSummaryEarly)
        {
            await store.UpdateProjection(stream, new OrderSummaryId(orderId), cancellationToken);
        }

        report.Add(new SeededModel("streamed", "aggregate", nameof(Order),
            nameof(OrderId), orderId,
            () => MeasureAggregate(store, stream, new OrderId(orderId))));

        report.Add(new SeededModel("streamed", "projection", nameof(OrderSummary),
            nameof(OrderSummaryId), orderId,
            () => MeasureProjection(store, stream, new OrderSummaryId(orderId))));
    }

    /// <summary>
    /// One order's life, as a list of calls on the aggregate. Each of them produces exactly one
    /// event, which is what lets the caller decide how many of them the snapshot is written at.
    /// </summary>
    private static List<Func<Order, string?>> Script(
        string orderId, string customerId, Random random, TimeProvider time)
    {
        var placedOn = RecentMoment(random, time);
        var lines = Enumerable.Range(0, random.Next(1, 4))
            .Select(_ =>
            {
                var product = ProductName(random);
                return (Sku: Sku(random, product), Quantity: random.Next(1, 4), Price: Price(random, 5, 250));
            })
            .DistinctBy(line => line.Sku)
            .ToList();

        var steps = new List<Func<Order, string?>>
        {
            order => order.Place(orderId, customerId, placedOn)
        };

        steps.AddRange(lines.Select<(string Sku, int Quantity, decimal Price), Func<Order, string?>>(
            line => order => order.AddItem(line.Sku, line.Quantity, line.Price)));

        // A change of mind before paying, which is the only point at which the order can take one.
        if (lines[0].Quantity > 1 && Chance(random, 30))
        {
            steps.Add(order => order.RemoveItem(lines[0].Sku, 1));
        }

        if (Chance(random, 15))
        {
            var reason = CancellationReason(random);
            steps.Add(order => order.Cancel(reason));
            return steps;
        }

        if (!Chance(random, 80))
        {
            // Left sitting in the basket, which is a perfectly ordinary thing for an order to do and
            // gives the read models something other than a finished sale to show.
            return steps;
        }

        steps.Add(order => order.Pay($"pay-{Id(random, "ref")}"));

        if (!Chance(random, 75))
        {
            return steps;
        }

        var warehouse = Warehouse(random);
        var carrier = Carrier(random);
        steps.Add(order => order.Despatch(warehouse, carrier, $"{carrier[..2].ToUpperInvariant()}{random.Next(100000, 999999)}"));

        if (!Chance(random, 70))
        {
            return steps;
        }

        var deliveredOn = placedOn.AddDays(random.Next(1, 6));
        steps.Add(order => order.Deliver(deliveredOn));

        if (Chance(random, 25))
        {
            var returned = lines[^1];
            steps.Add(order => order.Return(returned.Sku, 1, returned.Price));
        }

        return steps;
    }

    /// <summary>
    /// Runs the steps, refusing to carry on if the domain refuses one.
    /// </summary>
    /// <remarks>
    /// A refusal here is a bug in the script rather than a fact about the data: the counting that
    /// decides where the snapshot goes assumes one event per step, and a step that produced none
    /// would put the snapshot somewhere other than where this says it is.
    /// </remarks>
    private static void Drive(Order order, IEnumerable<Func<Order, string?>> steps)
    {
        foreach (var step in steps)
        {
            var refusal = step(order);

            if (refusal is not null)
            {
                throw new InvalidOperationException($"The sample order script was refused: {refusal}");
            }
        }
    }

    private static async Task<int> LatestSequence(
        IDomainService store, IStreamId stream, CancellationToken cancellationToken)
    {
        var result = await store.GetLatestEventSequence(stream, cancellationToken: cancellationToken);
        Check(result);

        return result.Value;
    }

    private static async Task<(int Snapshot, int Folded)> MeasureAggregate<T>(
        IDomainService store, IStreamId stream, IAggregateId<T> aggregateId)
        where T : IAggregateRoot, new()
    {
        var snapshot = (await store.GetAggregate(stream, aggregateId)).Value;
        var folded = (await store.GetInMemoryAggregate(stream, aggregateId)).Value;

        return (snapshot?.Version ?? 0, folded?.Version ?? 0);
    }

    private static async Task<(int Snapshot, int Folded)> MeasureProjection<T>(
        IDomainService store, IStreamId stream, IProjectionId<T> projectionId)
        where T : IProjection, new()
    {
        var snapshot = (await store.GetProjection(stream, projectionId)).Value;
        var folded = (await store.GetInMemoryProjection(stream, projectionId)).Value;

        return (snapshot?.Version ?? 0, folded?.Version ?? 0);
    }

    /// <summary>
    /// Stops the run on a failed write rather than carrying on and reporting numbers that are not
    /// what is in the store.
    /// </summary>
    private static void Check(Result result)
    {
        if (result.IsNotSuccess)
        {
            throw new InvalidOperationException(
                $"{result.Failure!.Title}: {result.Failure.Description}");
        }
    }

    private static void Check<T>(Result<T> result)
    {
        if (result.IsNotSuccess)
        {
            throw new InvalidOperationException(
                $"{result.Failure!.Title}: {result.Failure.Description}");
        }
    }
}
