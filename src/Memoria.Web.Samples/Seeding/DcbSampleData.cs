using Memoria.EventSourcing.Dcb;
using Memoria.Results;
using Memoria.Web.Samples.Dcb.Aggregates;
using Memoria.Web.Samples.Dcb.Events;
using Memoria.Web.Samples.Dcb.Projections;
using static Memoria.Web.Samples.Seeding.SampleVocabulary;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Writes a catalogue, stock for it, and orders holding some of that stock.
/// </summary>
/// <remarks>
/// <para>
/// Every append follows the read-decide-append cycle the DCB model is built around: read where the
/// boundary stands, fold it, decide, then append on condition that it has not moved. Nothing here
/// appends unconditionally, so the sample data is written the way an application would write it.
/// </para>
/// <para>
/// The run happens in phases so that snapshots can be left in three states. Models refreshed in the
/// middle phase have events appended after them and end up behind; models refreshed at the end are
/// current; models refreshed in neither phase have no snapshot at all. All three are worth having in
/// front of the web tool.
/// </para>
/// </remarks>
public static class DcbSampleData
{
    private sealed record SeededProduct(string ProductId, string Sku, string Name, decimal Price);

    public static async Task Add(
        IDcbDomainService dcb,
        Random random,
        SeedReport report,
        CancellationToken cancellationToken = default)
    {
        var products = new List<SeededProduct>();

        for (var index = 0; index < random.Next(3, 7); index++)
        {
            products.Add(await AddProduct(dcb, random, report, cancellationToken));
        }

        var orders = new List<string>();

        for (var index = 0; index < random.Next(2, 6); index++)
        {
            orders.Add(await AddOrder(dcb, products, random, report, cancellationToken));
        }

        // The middle of the run. What is refreshed here is snapshotted before the last phase appends
        // anything, so every one of these ends up behind.
        await Refresh(dcb, products, orders, random, percent: 45, cancellationToken);

        var lateOrder = await AddLaterEvents(dcb, products, orders, random, report, cancellationToken);

        // And the end of it. What is refreshed here has nothing appended after it, so it is current.
        // The late order is left out of both refreshes, so it holds events and no snapshot at all.
        await Refresh(dcb, products, orders, random, percent: 40, cancellationToken);

        Report(products, lateOrder is null ? orders : [..orders, lateOrder], dcb, report);
    }

    /// <summary>
    /// Creates a product and books its first deliveries in.
    /// </summary>
    /// <remarks>
    /// Creation reads the wider boundary — the product and its SKU — because that is the decision
    /// that has to see whether the code is already taken. Everything after it reads the product
    /// alone.
    /// </remarks>
    private static async Task<SeededProduct> AddProduct(
        IDcbDomainService dcb, Random random, SeedReport report, CancellationToken cancellationToken)
    {
        var name = ProductName(random);
        var product = new SeededProduct(Id(random, "p"), Sku(random, name), name, Price(random, 5, 250));

        var creationId = new ProductCreationId(product.ProductId, product.Sku);

        await Decide(dcb, creationId, report, cancellationToken,
            model => model.Create(product.ProductId, product.Name, product.Sku, product.Price));

        var stockId = new StockLevelId(product.ProductId);

        foreach (var _ in Enumerable.Range(0, random.Next(1, 4)))
        {
            var quantity = random.Next(5, 40);
            await Decide(dcb, stockId, report, cancellationToken,
                model => model.Replenish(quantity, $"gr-{Id(random, "n")}"));
        }

        return product;
    }

    /// <summary>
    /// Reserves stock for one order, a line at a time.
    /// </summary>
    /// <remarks>
    /// Folded by hand rather than through <c>GetInMemoryAggregate</c>, because the decision has to
    /// know which product and which order it is about before it applies anything and that method
    /// constructs the model itself. This is the shape every decision spanning two entities takes.
    /// </remarks>
    private static async Task<string> AddOrder(
        IDcbDomainService dcb,
        IReadOnlyList<SeededProduct> products,
        Random random,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        var orderId = Id(random, "o");

        foreach (var product in Sample(products, random.Next(1, 4), random))
        {
            await Reserve(dcb, product.ProductId, orderId, random.Next(1, 4), report, cancellationToken);
        }

        return orderId;
    }

    /// <summary>
    /// Everything that happens to the catalogue and the shelves after the first orders are in.
    /// </summary>
    /// <remarks>
    /// The point of this phase is that it appends inside boundaries that already have snapshots, so
    /// whatever was refreshed before it falls behind. What it appends is ordinary — a price change, a
    /// pick, a stock count — because a snapshot goes stale through ordinary business, not through
    /// anything special.
    /// </remarks>
    /// <returns>The order placed at the very end, if one was, so it can be reported.</returns>
    private static async Task<string?> AddLaterEvents(
        IDcbDomainService dcb,
        IReadOnlyList<SeededProduct> products,
        IReadOnlyList<string> orders,
        Random random,
        SeedReport report,
        CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            var productId = new ProductId(product.ProductId);

            // Half of these are appended without a snapshot, which is the only way a product's own
            // write model ends up behind: nothing else in this store writes an event it folds.
            var snapshot = Chance(random, 50);

            if (Chance(random, 55))
            {
                var price = Price(random, 5, 250);
                await Decide(dcb, productId, report, cancellationToken,
                    model => model.ChangeDetails(product.Name, price), snapshot);
            }

            if (Chance(random, 25))
            {
                var reason = DiscontinuationReason(random);
                await Decide(dcb, productId, report, cancellationToken,
                    model => model.Discontinue(reason), snapshot);
            }

            if (Chance(random, 35))
            {
                var difference = random.Next(-3, 4);
                var reason = AdjustmentReason(random);
                await Decide(dcb, new StockLevelId(product.ProductId), report, cancellationToken,
                    model => model.Adjust(difference, reason));
            }
        }

        foreach (var orderId in orders)
        {
            var holdings = (await dcb.GetInMemoryProjection(new OrderHoldingsId(orderId), cancellationToken)).Value!;

            foreach (var (productId, quantity) in holdings.Reserved)
            {
                if (Chance(random, 45))
                {
                    // Picked: the stock leaves, and both the product's boundary and the order's see it.
                    await Decide(dcb, new StockLevelId(productId), report, cancellationToken,
                        model => model.Pick(orderId, quantity));
                }
                else if (Chance(random, 25))
                {
                    await Release(dcb, productId, orderId, report, cancellationToken);
                }
            }
        }

        // A late order, so the last events in the log are not all corrections. It is placed after
        // the middle refresh and left out of the final one, which is how a run ends up with a
        // boundary holding events and no snapshot at all.
        if (!Chance(random, 60))
        {
            return null;
        }

        var latest = Id(random, "o");

        foreach (var product in Sample(products, random.Next(1, 3), random))
        {
            await Reserve(dcb, product.ProductId, latest, random.Next(1, 3), report, cancellationToken);
        }

        return latest;
    }

    /// <summary>
    /// Writes snapshots for a share of what has been seeded so far.
    /// </summary>
    /// <remarks>
    /// Through <c>UpdateAggregate</c> and <c>UpdateProjection</c>, which is the operation the web
    /// tool's refresh calls: read the latest snapshot, fold what arrived inside the boundary since,
    /// write it back. Called twice in a run, so which phase a model is picked in decides whether it
    /// ends up behind or current.
    /// </remarks>
    private static async Task Refresh(
        IDcbDomainService dcb,
        IReadOnlyList<SeededProduct> products,
        IReadOnlyList<string> orders,
        Random random,
        int percent,
        CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            if (Chance(random, percent))
            {
                await dcb.UpdateAggregate(new ProductId(product.ProductId), cancellationToken);
            }

            if (Chance(random, percent))
            {
                await dcb.UpdateAggregate(new StockLevelId(product.ProductId), cancellationToken);
            }

            if (Chance(random, percent))
            {
                await dcb.UpdateProjection(new ProductStockId(product.ProductId), cancellationToken);
            }
        }

        foreach (var orderId in orders)
        {
            if (Chance(random, percent))
            {
                await dcb.UpdateProjection(new OrderHoldingsId(orderId), cancellationToken);
            }

            foreach (var product in products.Where(_ => Chance(random, percent / 2)))
            {
                await dcb.UpdateProjection(
                    new OrderLineReservationId(product.ProductId, orderId), cancellationToken);
            }
        }
    }

    private static void Report(
        IReadOnlyList<SeededProduct> products,
        IReadOnlyList<string> orders,
        IDcbDomainService dcb,
        SeedReport report)
    {
        foreach (var product in products)
        {
            report.Add(new SeededModel("dcb", "aggregate", nameof(Dcb.Aggregates.Product),
                nameof(ProductId), product.ProductId,
                () => MeasureAggregate(dcb, new ProductId(product.ProductId))));

            report.Add(new SeededModel("dcb", "aggregate", nameof(StockLevel),
                nameof(StockLevelId), product.ProductId,
                () => MeasureAggregate(dcb, new StockLevelId(product.ProductId))));

            report.Add(new SeededModel("dcb", "projection", nameof(ProductStock),
                nameof(ProductStockId), product.ProductId,
                () => MeasureProjection(dcb, new ProductStockId(product.ProductId))));
        }

        foreach (var orderId in orders)
        {
            report.Add(new SeededModel("dcb", "projection", nameof(OrderHoldings),
                nameof(OrderHoldingsId), orderId,
                () => MeasureProjection(dcb, new OrderHoldingsId(orderId))));
        }
    }

    /// <summary>
    /// Reads a boundary, folds it into the model the identifier names, lets the caller decide, and
    /// appends what the decision staged.
    /// </summary>
    /// <remarks>
    /// The position is read before the fold rather than after it. It is a claim about what this
    /// decision saw, so reading it afterwards would let an event slip in between and be counted as
    /// seen when it was not.
    /// </remarks>
    /// <param name="snapshot">
    /// Whether to write a snapshot alongside the events. <c>SaveAggregate</c> does both;
    /// <c>SaveEvents</c> appends and leaves whatever snapshot exists where it was, which is how a
    /// write model that was current becomes one with an update waiting for it.
    /// </param>
    private static async Task Decide<T>(
        IDcbDomainService dcb,
        IDcbAggregateId<T> aggregateId,
        SeedReport report,
        CancellationToken cancellationToken,
        Func<T, string?> decide,
        bool snapshot = true)
        where T : class, IDcbAggregateRoot, new()
    {
        var position = await LatestPosition(dcb, aggregateId.Boundary, cancellationToken);

        var modelResult = await dcb.GetInMemoryAggregate(aggregateId, cancellationToken);
        Check(modelResult);

        var model = modelResult.Value!;
        var refusal = decide(model);

        if (refusal is not null)
        {
            // A refusal is the domain working, not a fault: a discontinued product cannot be
            // discontinued twice. The run carries on and simply appends nothing here.
            return;
        }

        var condition = new AppendCondition(aggregateId.Boundary, position);

        Check(snapshot
            ? await dcb.SaveAggregate(aggregateId, model, condition, cancellationToken)
            : await dcb.SaveEvents([..model.UncommittedEvents], condition, cancellationToken));

        report.Appended(model.UncommittedEvents.Count);
    }

    private static async Task Reserve(
        IDcbDomainService dcb,
        string productId,
        string orderId,
        int quantity,
        SeedReport report,
        CancellationToken cancellationToken) =>
        await DecideAcrossTwo(dcb, productId, orderId, report, cancellationToken,
            decision => decision.Reserve(quantity));

    private static async Task Release(
        IDcbDomainService dcb,
        string productId,
        string orderId,
        SeedReport report,
        CancellationToken cancellationToken) =>
        await DecideAcrossTwo(dcb, productId, orderId, report, cancellationToken,
            decision => decision.Release());

    /// <summary>
    /// The read-decide-append cycle for a decision spanning a product and an order.
    /// </summary>
    /// <remarks>
    /// Folded by hand and appended with <c>SaveEvents</c> rather than <c>SaveAggregate</c>: the
    /// model has to be told which pair it is about before it applies anything, and the decision is
    /// thrown away afterwards rather than snapshotted. What it appends does move both boundaries,
    /// which is what leaves the product's and the order's own models behind.
    /// </remarks>
    private static async Task DecideAcrossTwo(
        IDcbDomainService dcb,
        string productId,
        string orderId,
        SeedReport report,
        CancellationToken cancellationToken,
        Func<StockReservationDecision, string?> decide)
    {
        var decisionId = new StockReservationDecisionId(productId, orderId);
        var boundary = decisionId.Boundary;

        var decision = new StockReservationDecision().About(productId, orderId);

        var position = await LatestPosition(dcb, boundary, cancellationToken);

        var eventsResult = await dcb.GetEvents(boundary, decision.EventTypeFilter, cancellationToken);
        Check(eventsResult);

        decision.Apply(eventsResult.Value!);

        if (decide(decision) is not null)
        {
            return;
        }

        Check(await dcb.SaveEvents([..decision.UncommittedEvents],
            new AppendCondition(boundary, position), cancellationToken));

        report.Appended(decision.UncommittedEvents.Count);
    }

    private static async Task<long> LatestPosition(
        IDcbDomainService dcb, TagQuery boundary, CancellationToken cancellationToken)
    {
        var result = await dcb.GetLatestPosition(boundary, cancellationToken: cancellationToken);
        Check(result);

        return result.Value;
    }

    private static async Task<(int Snapshot, int Folded)> MeasureAggregate<T>(
        IDcbDomainService dcb, IDcbAggregateId<T> aggregateId) where T : class, IDcbAggregateRoot, new()
    {
        var snapshot = (await dcb.GetAggregate(aggregateId)).Value;
        var folded = (await dcb.GetInMemoryAggregate(aggregateId)).Value;

        return (snapshot?.Version ?? 0, folded?.Version ?? 0);
    }

    private static async Task<(int Snapshot, int Folded)> MeasureProjection<T>(
        IDcbDomainService dcb, IDcbProjectionId<T> projectionId) where T : class, IDcbProjection, new()
    {
        var snapshot = (await dcb.GetProjection(projectionId)).Value;
        var folded = (await dcb.GetInMemoryProjection(projectionId)).Value;

        return (snapshot?.Version ?? 0, folded?.Version ?? 0);
    }

    /// <summary>
    /// Takes a few of the products, without taking one twice.
    /// </summary>
    private static IEnumerable<SeededProduct> Sample(
        IReadOnlyList<SeededProduct> products, int count, Random random) =>
        products.OrderBy(_ => random.Next()).Take(Math.Min(count, products.Count)).ToList();

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
