using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Aggregates;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Projections;

/// <summary>
/// One order as a screen wants it: where it is, what is on it, what it came to.
/// </summary>
/// <remarks>
/// <para>
/// A projection differs from an aggregate in one way only — it never produces events, so it has no
/// <c>Add</c> and no uncommitted events. Everything else is the same fold, over the same stream,
/// narrowed by the same event property filter.
/// </para>
/// <para>
/// It overlaps <see cref="Order"/> on purpose, and the overlap is not duplication. The aggregate
/// carries what its rules need and is folded fresh for a decision; this carries what the order page
/// shows — a carrier, a tracking reference, a line count — and is stored as a snapshot so the page
/// does not start from the beginning of the customer's history every time it is opened.
/// </para>
/// </remarks>
[ProjectionType("OrderSummary")]
public class OrderSummary : Projection
{
    public string OrderId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; } = OrderStatus.None;

    public DateTimeOffset PlacedOn { get; private set; }

    public DateTimeOffset? DeliveredOn { get; private set; }

    /// <summary>
    /// How many of each SKU are on the order.
    /// </summary>
    /// <remarks>
    /// A private setter over a whole new dictionary each time, rather than a mutable one that is
    /// added to. A projection is stored and read back, so whatever holds its state has to survive
    /// the round trip: the serializer writes every public getter but can only restore what it can
    /// set.
    /// </remarks>
    public IReadOnlyDictionary<string, int> Quantities { get; private set; } =
        new Dictionary<string, int>();

    public decimal Total { get; private set; }

    public decimal Refunded { get; private set; }

    public string? Carrier { get; private set; }

    public string? TrackingReference { get; private set; }

    public string? CancellationReason { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(OrderPlacedEvent),
        typeof(OrderItemAddedEvent),
        typeof(OrderItemRemovedEvent),
        typeof(OrderPaidEvent),
        typeof(OrderCancelledEvent),
        typeof(OrderDespatchedEvent),
        typeof(OrderDeliveredEvent),
        typeof(OrderItemReturnedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                OrderId = placed.OrderId;
                CustomerId = placed.CustomerId;
                PlacedOn = placed.PlacedOn;
                Status = OrderStatus.Placed;
                return true;

            case OrderItemAddedEvent added:
                Quantities = With(Quantities, added.Sku, added.Quantity);
                Total += added.Quantity * added.UnitPrice;
                return true;

            case OrderItemRemovedEvent removed:
                Quantities = With(Quantities, removed.Sku, -removed.Quantity);
                Total -= removed.Quantity * removed.UnitPrice;
                return true;

            case OrderPaidEvent paid:
                Status = OrderStatus.Paid;
                Total = paid.Amount;
                return true;

            case OrderCancelledEvent cancelled:
                Status = OrderStatus.Cancelled;
                CancellationReason = cancelled.Reason;
                return true;

            case OrderDespatchedEvent despatched:
                Status = OrderStatus.Despatched;
                Carrier = despatched.Carrier;
                TrackingReference = despatched.TrackingReference;
                return true;

            case OrderDeliveredEvent delivered:
                Status = OrderStatus.Delivered;
                DeliveredOn = delivered.DeliveredOn;
                return true;

            case OrderItemReturnedEvent returned:
                Quantities = With(Quantities, returned.Sku, -returned.Quantity);
                Refunded += returned.Refund;
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> quantities, string sku, int quantity)
    {
        var updated = new Dictionary<string, int>(quantities);
        var total = updated.GetValueOrDefault(sku) + quantity;

        if (total > 0)
        {
            updated[sku] = total;
        }
        else
        {
            updated.Remove(sku);
        }

        return updated;
    }
}

/// <summary>
/// Identifies the summary, and therefore its snapshot, and narrows the customer's stream to the one
/// order it is about.
/// </summary>
/// <remarks>
/// The same filter <see cref="OrderId"/> carries, for the same reason: a read model is no less
/// likely to share a stream than a write model, so it narrows the same way.
/// </remarks>
public class OrderSummaryId(string orderId) : IProjectionId<OrderSummary>
{
    public string Id { get; } = orderId;

    public IDictionary<string, string>? EventPropertyFilter { get; } =
        new Dictionary<string, string> { ["OrderId"] = orderId };
}
