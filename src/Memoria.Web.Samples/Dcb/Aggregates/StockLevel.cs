using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Aggregates;

/// <summary>
/// How much of one product there is, how much is spoken for, and how much is left.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to <see cref="StockReservationDecision"/>, and the reason both are here. A
/// reservation spans a product <em>and</em> an order, so the caller folds it by hand and appends
/// with <c>SaveEvents</c>. A stock level is identified by its own product and reads one tag, so the
/// store can build it, fold it and keep a snapshot of it — which is what <c>GetAggregate</c> and
/// <c>SaveAggregate</c> are for.
/// </para>
/// <para>
/// Its boundary is <c>product:{id}</c> alone, so every event it reads is already about this product
/// and <c>Apply</c> never has to check. <see cref="StockReservationDecision"/> does have to, because
/// its boundary spans two entities and one reservation event means different things seen through
/// each.
/// </para>
/// <para>
/// It reads the reservations as well as the deliveries, and that is what makes the boundary worth
/// having: without them a count could be written off against stock that is already promised.
/// </para>
/// </remarks>
[AggregateType("StockLevel")]
public class StockLevel : DcbAggregateRoot
{
    /// <summary>What has come in, less what has been picked and any correction.</summary>
    public int OnHand { get; private set; }

    /// <summary>Held for orders that have not been picked yet.</summary>
    public int Reserved { get; private set; }

    /// <summary>What is left to promise anybody.</summary>
    public int Available => OnHand - Reserved;

    public int Deliveries { get; private set; }

    /// <summary>
    /// The catalogue events are outside this filter, exactly as the stock events are outside
    /// <see cref="Product.EventTypeFilter"/>. Two models, one tag, neither reading the other's half.
    /// </summary>
    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(StockReplenishedEvent),
        typeof(StockReservedEvent),
        typeof(StockReservationReleasedEvent),
        typeof(StockPickedEvent),
        typeof(StockAdjustedEvent)
    ];

    /// <summary>
    /// The product this fold is about, taken from the boundary. One tag, because the boundary is
    /// one product.
    /// </summary>
    private string ProductCode => Tags.Single(tag => tag.Key == "product").Value;

    /// <summary>
    /// Books a delivery in, or explains why it cannot be booked.
    /// </summary>
    public string? Replenish(int quantity, string goodsInReference)
    {
        if (quantity < 1) return $"A delivery has to bring at least one unit, not {quantity}.";

        // Staged with no tags of its own, so it inherits the aggregate's — which the store set from
        // the boundary. For a model that reads exactly one tag that is exactly the right tag.
        Add(new StockReplenishedEvent(ProductCode, quantity, goodsInReference));

        return null;
    }

    /// <summary>
    /// Records that reserved stock has actually left, or explains why it cannot have.
    /// </summary>
    /// <remarks>
    /// Tagged with the order as well, because the order's side of the story needs it: a fold over
    /// <c>order:{id}</c> that could not see the pick would go on thinking the stock was merely
    /// reserved.
    /// </remarks>
    public string? Pick(string orderId, int quantity)
    {
        if (quantity < 1) return $"A pick has to be at least one unit, not {quantity}.";

        if (quantity > Reserved)
        {
            return $"Only {Reserved} of '{ProductCode}' is reserved, so {quantity} cannot be picked.";
        }

        Add(new StockPickedEvent(ProductCode, orderId, quantity),
            new Tag("product", ProductCode), new Tag("order", orderId));

        return null;
    }

    /// <summary>
    /// Writes a count difference into the log, or explains why it cannot be written.
    /// </summary>
    /// <remarks>
    /// Refuses to take the shelf below what is already promised. This is the rule that makes the
    /// boundary include reservations rather than deliveries alone.
    /// </remarks>
    public string? Adjust(int difference, string reason)
    {
        if (difference == 0) return "An adjustment of nothing changes nothing.";

        if (OnHand + difference < Reserved)
        {
            return $"'{ProductCode}' has {Reserved} units reserved, " +
                   $"so the count cannot drop to {OnHand + difference}.";
        }

        Add(new StockAdjustedEvent(ProductCode, difference, reason));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case StockReplenishedEvent replenished:
                OnHand += replenished.Quantity;
                Deliveries++;
                return true;

            case StockReservedEvent reserved:
                Reserved += reserved.Quantity;
                return true;

            case StockReservationReleasedEvent released:
                Reserved -= released.Quantity;
                return true;

            case StockPickedEvent picked:
                Reserved -= picked.Quantity;
                OnHand -= picked.Quantity;
                return true;

            case StockAdjustedEvent adjusted:
                OnHand += adjusted.Difference;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the stock level, and therefore its snapshot, and carries the boundary it is folded
/// from.
/// </summary>
/// <remarks>
/// <para>
/// One tag — the same tag <see cref="ProductId"/> uses. Two models can share a boundary without
/// sharing anything else; what they take from it is decided by their event type filters.
/// </para>
/// <para>
/// The id is not the bare product code, and that is not decoration. A snapshot is identified by its
/// kind, its store id — <c>{Id}:{type version}</c> — and a digest of its boundary, and the model
/// type is in none of them. Two write models folded from the same boundary under the same id would
/// therefore be the same row, and would overwrite each other. Prefixing the id keeps
/// <see cref="StockLevel"/>'s snapshot separate from <see cref="Product"/>'s.
/// </para>
/// </remarks>
public class StockLevelId(string productId) : IDcbAggregateId<StockLevel>
{
    public string Id { get; } = $"stock-{productId}";

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("product", productId));
}
