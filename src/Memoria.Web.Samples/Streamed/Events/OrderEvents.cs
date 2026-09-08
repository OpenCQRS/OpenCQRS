using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Events;

/// <summary>
/// A customer placed an order.
/// </summary>
/// <remarks>
/// Every event in this file carries <c>OrderId</c> as its first value, and that is not decoration.
/// The customer's stream holds all of their orders, so a model about one order narrows the stream
/// with <c>EventPropertyFilter["OrderId"]</c> — which can only find a property that is there. See
/// <see cref="Aggregates.OrderId"/>.
/// </remarks>
[EventType("OrderPlaced")]
public record OrderPlacedEvent(string OrderId, string CustomerId, DateTimeOffset PlacedOn) : IEvent;

/// <summary>
/// A line was added to an order that had not yet been paid for.
/// </summary>
[EventType("OrderItemAdded")]
public record OrderItemAddedEvent(string OrderId, string Sku, int Quantity, decimal UnitPrice) : IEvent;

/// <summary>
/// A line was taken off the order before payment.
/// </summary>
/// <remarks>
/// Carries the quantity removed rather than the quantity remaining, so the fold subtracts rather
/// than being told an answer it would then have to trust — and the price it came off at, so a
/// model tracking money does not have to go looking for the line it belonged to.
/// </remarks>
[EventType("OrderItemRemoved")]
public record OrderItemRemovedEvent(string OrderId, string Sku, int Quantity, decimal UnitPrice) : IEvent;

/// <summary>
/// The order was paid for.
/// </summary>
/// <remarks>
/// The amount is recorded even though the lines already imply it. What was charged is a fact about
/// the payment, and a later correction to a price should not quietly restate what the customer was
/// asked to pay.
/// </remarks>
[EventType("OrderPaid")]
public record OrderPaidEvent(string OrderId, decimal Amount, string PaymentReference) : IEvent;

/// <summary>
/// The order was cancelled before it shipped.
/// </summary>
[EventType("OrderCancelled")]
public record OrderCancelledEvent(string OrderId, string Reason) : IEvent;
