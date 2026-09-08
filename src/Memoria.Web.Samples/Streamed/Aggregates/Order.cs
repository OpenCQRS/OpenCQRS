using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Aggregates;

/// <summary>
/// One order, folded out of the customer's stream.
/// </summary>
/// <remarks>
/// <para>
/// The write model for everything that happens to a single order. It is not the only model built
/// from that stream — <see cref="CustomerAccount"/> reads the same events and answers a different
/// question — which is the point of a stream that belongs to the customer rather than to any one
/// model.
/// </para>
/// <para>
/// Every decision here is one an order can make on its own: whether a line can still be added,
/// whether there is anything to pay for, whether it is too late to cancel. A rule spanning the
/// order and something else — is there stock for this line? — cannot be answered from this fold,
/// and belongs on the DCB side under <c>Dcb/</c>.
/// </para>
/// </remarks>
[AggregateType("Order")]
public class Order : AggregateRoot
{
    public bool Exists { get; private set; }

    public string OrderId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; } = OrderStatus.None;

    public IReadOnlyList<OrderLine> Lines { get; private set; } = [];

    public decimal Total => Lines.Sum(line => line.Quantity * line.UnitPrice);

    /// <summary>
    /// What was actually charged, which stops being the same as <see cref="Total"/> the moment
    /// something is returned.
    /// </summary>
    public decimal Paid { get; private set; }

    public decimal Refunded { get; private set; }

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

    /// <summary>
    /// Opens the order, or explains why it cannot be opened.
    /// </summary>
    public string? Place(string orderId, string customerId, DateTimeOffset placedOn)
    {
        if (Exists) return $"Order '{orderId}' has already been placed.";

        Add(new OrderPlacedEvent(orderId, customerId, placedOn));

        return null;
    }

    /// <summary>
    /// Adds a line, or explains why it cannot be added.
    /// </summary>
    public string? AddItem(string sku, int quantity, decimal unitPrice)
    {
        if (!Exists) return "That order has not been placed.";
        if (quantity < 1) return $"Quantity must be at least one, not {quantity}.";
        if (unitPrice < 0) return "A price cannot be negative.";

        if (Status != OrderStatus.Placed)
        {
            return $"An order that is {Describe(Status)} cannot take another line.";
        }

        Add(new OrderItemAddedEvent(OrderId, sku, quantity, unitPrice));

        return null;
    }

    /// <summary>
    /// Takes a quantity off a line, or explains why it cannot.
    /// </summary>
    public string? RemoveItem(string sku, int quantity)
    {
        if (Status != OrderStatus.Placed)
        {
            return $"An order that is {Describe(Status)} can no longer be edited.";
        }

        var line = Lines.SingleOrDefault(candidate => candidate.Sku == sku);
        if (line is null) return $"There is no '{sku}' on this order.";
        if (quantity > line.Quantity) return $"Only {line.Quantity} of '{sku}' is on the order.";

        Add(new OrderItemRemovedEvent(OrderId, sku, quantity, line.UnitPrice));

        return null;
    }

    /// <summary>
    /// Takes payment, or explains why it cannot be taken.
    /// </summary>
    /// <remarks>
    /// The amount is not a parameter. What is owed is what this fold says it is, and letting the
    /// caller pass a number would make the event a record of what someone claimed rather than of
    /// what the order came to.
    /// </remarks>
    public string? Pay(string paymentReference)
    {
        if (!Exists) return "That order has not been placed.";
        if (Status != OrderStatus.Placed) return $"That order is already {Describe(Status)}.";
        if (Lines.Count == 0) return "An empty order cannot be paid for.";

        Add(new OrderPaidEvent(OrderId, Total, paymentReference));

        return null;
    }

    /// <summary>
    /// Cancels the order, or explains why it is too late.
    /// </summary>
    public string? Cancel(string reason)
    {
        if (!Exists) return "That order has not been placed.";
        if (Status == OrderStatus.Cancelled) return "That order is already cancelled.";

        if (Status is OrderStatus.Despatched or OrderStatus.Delivered)
        {
            return $"That order is {Describe(Status)}, so it can only be returned.";
        }

        Add(new OrderCancelledEvent(OrderId, reason));

        return null;
    }

    /// <summary>
    /// Sends the order out, or explains why it cannot go.
    /// </summary>
    /// <remarks>
    /// Staged here but appended to the warehouse's stream as well as the customer's, which the
    /// caller does — an aggregate stages events, it does not decide where they are written.
    /// </remarks>
    public string? Despatch(string warehouseCode, string carrier, string trackingReference)
    {
        if (Status == OrderStatus.Cancelled) return "That order was cancelled.";
        if (Status != OrderStatus.Paid) return $"An order that is {Describe(Status)} cannot be sent out.";

        Add(new OrderDespatchedEvent(OrderId, warehouseCode, carrier, trackingReference));

        return null;
    }

    /// <summary>
    /// Records a return, or explains why there is nothing to return.
    /// </summary>
    public string? Return(string sku, int quantity, decimal refund)
    {
        if (Status is not (OrderStatus.Despatched or OrderStatus.Delivered))
        {
            return $"An order that is {Describe(Status)} has nothing to return.";
        }

        var line = Lines.SingleOrDefault(candidate => candidate.Sku == sku);
        if (line is null) return $"There is no '{sku}' on this order.";
        if (quantity > line.Quantity) return $"Only {line.Quantity} of '{sku}' was sent.";

        Add(new OrderItemReturnedEvent(OrderId, sku, quantity, refund));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                Exists = true;
                OrderId = placed.OrderId;
                CustomerId = placed.CustomerId;
                Status = OrderStatus.Placed;
                return true;

            case OrderItemAddedEvent added:
                Lines = Merge(Lines, added.Sku, added.Quantity, added.UnitPrice);
                return true;

            case OrderItemRemovedEvent removed:
                Lines = Merge(Lines, removed.Sku, -removed.Quantity, unitPrice: null);
                return true;

            case OrderPaidEvent paid:
                Status = OrderStatus.Paid;
                Paid = paid.Amount;
                return true;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                return true;

            case OrderDespatchedEvent:
                Status = OrderStatus.Despatched;
                return true;

            case OrderDeliveredEvent:
                Status = OrderStatus.Delivered;
                return true;

            case OrderItemReturnedEvent returned:
                Lines = Merge(Lines, returned.Sku, -returned.Quantity, unitPrice: null);
                Refunded += returned.Refund;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Adds a quantity to a line, or takes it away, dropping the line when nothing is left of it.
    /// </summary>
    /// <remarks>
    /// The list is replaced rather than edited in place, so nothing reading the model can catch a
    /// line halfway through being changed.
    /// </remarks>
    private static IReadOnlyList<OrderLine> Merge(
        IReadOnlyList<OrderLine> lines, string sku, int quantity, decimal? unitPrice)
    {
        var existing = lines.SingleOrDefault(line => line.Sku == sku);

        if (existing is null)
        {
            return quantity > 0 ? [..lines, new OrderLine(sku, quantity, unitPrice ?? 0)] : lines;
        }

        var merged = existing with
        {
            Quantity = existing.Quantity + quantity,
            UnitPrice = unitPrice ?? existing.UnitPrice
        };

        return merged.Quantity > 0
            ? [..lines.Select(line => line.Sku == sku ? merged : line)]
            : [..lines.Where(line => line.Sku != sku)];
    }

    private static string Describe(OrderStatus status) =>
        status == OrderStatus.None ? "not yet placed" : status.ToString().ToLowerInvariant();
}

/// <summary>
/// One line of an order.
/// </summary>
public record OrderLine(string Sku, int Quantity, decimal UnitPrice);

/// <summary>
/// Where an order has got to.
/// </summary>
public enum OrderStatus
{
    /// <summary>Nothing has happened yet — the fold found no events.</summary>
    None = 0,
    Placed = 1,
    Paid = 2,
    Despatched = 3,
    Delivered = 4,
    Cancelled = 5
}

/// <summary>
/// Identifies one order inside the stream that holds all of the customer's.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EventPropertyFilter"/> is what lets several orders share one stream. The stream
/// says which events to read; the filter says which of them are this order's, by matching the
/// <c>OrderId</c> property every event in <c>Streamed/Events</c> carries. Drop it and the fold
/// would take in the customer's other orders and quietly get every answer wrong.
/// </para>
/// <para>
/// <see cref="CustomerAccountId"/> is the same idea with the filter left off, because that model
/// does want the whole stream.
/// </para>
/// </remarks>
public class OrderId(string orderId) : IAggregateId<Order>
{
    public string Id { get; } = orderId;

    public IDictionary<string, string>? EventPropertyFilter { get; } =
        new Dictionary<string, string> { ["OrderId"] = orderId };
}
