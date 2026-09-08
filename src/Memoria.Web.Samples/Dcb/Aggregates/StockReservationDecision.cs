using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Aggregates;

/// <summary>
/// Everything one reservation decision needs to know, folded from the events of one product and one
/// order together.
/// </summary>
/// <remarks>
/// <para>
/// This is the shape a stream cannot hold. The rule is:
/// </para>
/// <list type="bullet">
/// <item><description>the product is in the catalogue, on sale, and has units free — a fact about the <em>product</em>;</description></item>
/// <item><description>the order is not already holding this product — a fact about <em>both</em>;</description></item>
/// <item><description>the order is holding fewer than <see cref="MaximumLinesPerOrder"/> lines — a fact about the <em>order</em>.</description></item>
/// </list>
/// <para>
/// A stream per product cannot see what else the order is holding; a stream per order cannot see how
/// much stock is left. Putting both in one stream serialises every reservation in the shop. Here the
/// boundary is the query <c>product:p1 OR order:o7</c>, so two reservations contend only when they
/// share a product or an order.
/// </para>
/// <para>
/// Compare <see cref="StockLevel"/>, which answers a narrower question over one tag and can
/// therefore be snapshotted by the store. This one is folded for a decision and thrown away.
/// </para>
/// </remarks>
[AggregateType("StockReservationDecision")]
public class StockReservationDecision : DcbAggregateRoot
{
    private readonly Dictionary<string, int> _heldByTheOrder = [];

    /// <summary>
    /// How many distinct products one order may hold at once. A basket limit, and the reason the
    /// order is in the boundary at all.
    /// </summary>
    public const int MaximumLinesPerOrder = 20;

    public string ProductId { get; private set; } = null!;

    public string OrderId { get; private set; } = null!;

    public bool ProductExists { get; private set; }

    public bool Discontinued { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available => OnHand - Reserved;

    public int LinesOnTheOrder => _heldByTheOrder.Count;

    public int AlreadyHeldForThisProduct => _heldByTheOrder.GetValueOrDefault(ProductId);

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductCreatedEvent),
        typeof(ProductDiscontinuedEvent),
        typeof(ProductDeletedEvent),
        typeof(StockReplenishedEvent),
        typeof(StockAdjustedEvent),
        typeof(StockReservedEvent),
        typeof(StockReservationReleasedEvent),
        typeof(StockPickedEvent)
    ];

    /// <summary>
    /// Names the product and order this decision is about, so the fold can tell "this product" from
    /// the order's other lines.
    /// </summary>
    public StockReservationDecision About(string productId, string orderId)
    {
        ProductId = productId;
        OrderId = orderId;
        return this;
    }

    /// <summary>
    /// Sets stock aside for the order, or explains why it cannot.
    /// </summary>
    public string? Reserve(int quantity)
    {
        if (quantity < 1) return $"A reservation has to be at least one unit, not {quantity}.";
        if (!ProductExists) return $"Product '{ProductId}' is not in the catalogue.";
        if (Discontinued) return $"Product '{ProductId}' is no longer on sale.";
        if (AlreadyHeldForThisProduct > 0) return $"Order '{OrderId}' is already holding '{ProductId}'.";

        if (LinesOnTheOrder >= MaximumLinesPerOrder)
        {
            return $"Order '{OrderId}' already has {LinesOnTheOrder} lines.";
        }

        if (quantity > Available)
        {
            return $"Only {Available} of '{ProductId}' is available, so {quantity} cannot be reserved.";
        }

        // Tagged with both, so it moves either boundary: a later decision about this product sees
        // it, and so does a later decision about this order.
        Add(new StockReservedEvent(ProductId, OrderId, quantity),
            new Tag("product", ProductId), new Tag("order", OrderId));

        return null;
    }

    /// <summary>
    /// Gives a reservation back, or explains why there is nothing to give back.
    /// </summary>
    public string? Release()
    {
        if (AlreadyHeldForThisProduct == 0) return $"Order '{OrderId}' is not holding '{ProductId}'.";

        Add(new StockReservationReleasedEvent(ProductId, OrderId, AlreadyHeldForThisProduct),
            new Tag("product", ProductId), new Tag("order", OrderId));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case ProductCreatedEvent created when created.ProductId == ProductId:
                ProductExists = true;
                Discontinued = false;
                return true;

            // Being taken off sale is as much a fact about the product as being created. Adding an
            // event type is never only about the model that emits it: every model whose decision
            // depends on it has to filter and apply it too, or it silently decides on stale facts.
            case ProductDiscontinuedEvent discontinued when discontinued.ProductId == ProductId:
                Discontinued = true;
                return true;

            case ProductDeletedEvent deleted when deleted.ProductId == ProductId:
                ProductExists = false;
                return true;

            case StockReplenishedEvent replenished when replenished.ProductId == ProductId:
                OnHand += replenished.Quantity;
                return true;

            case StockAdjustedEvent adjusted when adjusted.ProductId == ProductId:
                OnHand += adjusted.Difference;
                return true;

            case StockReservedEvent reserved:
                // One event, two meanings. Seen through the product tag it uses up stock; seen
                // through the order tag it fills one of the order's lines. The same event does both.
                if (reserved.ProductId == ProductId) Reserved += reserved.Quantity;
                if (reserved.OrderId == OrderId) Hold(reserved.ProductId, reserved.Quantity);
                return true;

            case StockReservationReleasedEvent released:
                if (released.ProductId == ProductId) Reserved -= released.Quantity;
                if (released.OrderId == OrderId) Hold(released.ProductId, -released.Quantity);
                return true;

            case StockPickedEvent picked:
                if (picked.ProductId == ProductId)
                {
                    Reserved -= picked.Quantity;
                    OnHand -= picked.Quantity;
                }

                if (picked.OrderId == OrderId) Hold(picked.ProductId, -picked.Quantity);
                return true;

            default:
                return false;
        }
    }

    private void Hold(string productId, int quantity)
    {
        var held = _heldByTheOrder.GetValueOrDefault(productId) + quantity;

        if (held > 0)
        {
            _heldByTheOrder[productId] = held;
        }
        else
        {
            _heldByTheOrder.Remove(productId);
        }
    }
}

/// <summary>
/// Identifies the decision, and therefore its snapshot. It does not select the events — the boundary
/// does that.
/// </summary>
public class StockReservationDecisionId(string productId, string orderId)
    : IDcbAggregateId<StockReservationDecision>
{
    public string Id { get; } = $"{productId}-{orderId}";

    /// <summary>
    /// Everything the decision reads, and nothing else — so a reservation of a different product by
    /// a different order never contends with this one.
    /// </summary>
    public TagQuery Boundary { get; } =
        TagQuery.AnyOf(new Tag("product", productId), new Tag("order", orderId));
}
