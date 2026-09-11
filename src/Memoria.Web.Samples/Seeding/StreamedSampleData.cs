using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;
using Memoria.Results;
using Memoria.Web.Samples.Streamed.Aggregates;
using Memoria.Web.Samples.Streamed.Events;
using Memoria.Web.Samples.Streamed.Projections;
using Memoria.Web.Samples.Streamed.Streams;
using static Memoria.Web.Samples.Seeding.SampleVocabulary;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Writes customers, their orders, the reviews products collect, and the models folded from them.
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

        var reviewedProducts = random.Next(1, 4);

        for (var product = 0; product < reviewedProducts; product++)
        {
            await AddProductReviews(store, random, report, cancellationToken);
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
    /// Fills one product's review stream: a handful of reviews, the votes other shoppers cast on
    /// them, and the summary folded from the lot.
    /// </summary>
    /// <remarks>
    /// The summary is refreshed after a randomly chosen review, so it ends up current, behind, or
    /// missing depending on where that lands — the same three states the customer's models are left
    /// in.
    /// </remarks>
    private static async Task AddProductReviews(
        IDomainService store,
        Random random,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        var productId = Id(random, "p");
        var stream = new ProductReviewStreamId(productId);
        var reviews = random.Next(1, 4);

        // Which review the whole-stream summary is refreshed after. Land on the last one and it
        // ends up current; land earlier and every review after it leaves it behind.
        var refreshSummaryAfter = random.Next(reviews);

        for (var review = 0; review < reviews; review++)
        {
            await AddReview(store, stream, productId, random, report, cancellationToken);

            if (review == refreshSummaryAfter)
            {
                await store.UpdateProjection(stream, new ProductRatingSummaryId(productId), cancellationToken);
            }
        }

        report.Add(new SeededModel("streamed", "projection", nameof(ProductRatingSummary),
            nameof(ProductRatingSummaryId), productId,
            () => MeasureProjection(store, stream, new ProductRatingSummaryId(productId))));
    }

    /// <summary>
    /// One review's life: written, sometimes revised, occasionally taken down, and voted on by
    /// other shoppers.
    /// </summary>
    /// <remarks>
    /// Most reviews are driven through the current <see cref="ProductReview"/>; a share are driven
    /// through <see cref="ProductReviewV1"/> instead, so the store holds snapshots of both shapes
    /// side by side and the old revision event next to the new — which is the whole point of the
    /// versioned pair. The votes are appended directly, because no write model stages them: a vote
    /// belongs to the shopper who cast it, not to the review's own decisions.
    /// </remarks>
    private static async Task AddReview(
        IDomainService store,
        ProductReviewStreamId stream,
        string productId,
        Random random,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        var reviewId = Id(random, "r");
        var customerId = Id(random, "c");
        var sequence = await LatestSequence(store, stream, cancellationToken);

        if (Chance(random, 30))
        {
            var oldReview = new ProductReviewV1();
            Allow(oldReview.Submit(reviewId, productId, customerId, random.Next(1, 6), ReviewBody(random)));

            if (Chance(random, 40))
            {
                Allow(oldReview.Revise(ReviewBody(random)));
            }

            var oldEvents = oldReview.UncommittedEvents.Count();
            Check(await store.SaveAggregate(stream, new ProductReviewV1Id(reviewId), oldReview, sequence, cancellationToken));
            report.Appended(oldEvents);

            report.Add(new SeededModel("streamed", "aggregate", nameof(ProductReviewV1),
                nameof(ProductReviewV1Id), reviewId,
                () => MeasureAggregate(store, stream, new ProductReviewV1Id(reviewId))));
        }
        else
        {
            var review = new ProductReview();
            Allow(review.Submit(reviewId, productId, customerId, random.Next(1, 6),
                ReviewTitle(random), ReviewBody(random)));

            if (Chance(random, 40))
            {
                Allow(review.Revise(random.Next(1, 6), ReviewTitle(random), ReviewBody(random)));
            }

            if (Chance(random, 15))
            {
                Allow(review.Remove(ReviewRemovalReason(random)));
            }

            // Half the reviews are snapshotted as they are written; the rest only append, which is
            // how a review's write model ends up with events waiting for it.
            var events = review.UncommittedEvents.ToArray();

            Check(Chance(random, 50)
                ? await store.SaveAggregate(stream, new ProductReviewId(reviewId), review, sequence, cancellationToken)
                : await store.SaveEvents(stream, events, sequence, cancellationToken));

            report.Appended(events.Length);

            report.Add(new SeededModel("streamed", "aggregate", nameof(ProductReview),
                nameof(ProductReviewId), reviewId,
                () => MeasureAggregate(store, stream, new ProductReviewId(reviewId))));
        }

        var votes = random.Next(0, 4);

        if (votes > 0)
        {
            var voteEvents = Enumerable.Range(0, votes)
                .Select(IEvent (_) => new ProductReviewVotedEvent(reviewId, productId, Chance(random, 70)))
                .ToArray();

            sequence = await LatestSequence(store, stream, cancellationToken);
            Check(await store.SaveEvents(stream, voteEvents, sequence, cancellationToken));
            report.Appended(votes);
        }
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

    /// <summary>
    /// Refuses to carry on if the domain refused a step, for the same reason <see cref="Drive"/>
    /// does: a refusal here is a bug in the script, not a fact about the data.
    /// </summary>
    private static void Allow(string? refusal)
    {
        if (refusal is not null)
        {
            throw new InvalidOperationException($"The sample review script was refused: {refusal}");
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
