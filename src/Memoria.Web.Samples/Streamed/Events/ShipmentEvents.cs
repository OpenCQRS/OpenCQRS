using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Events;

/// <summary>
/// The warehouse sent the order out.
/// </summary>
/// <remarks>
/// Appended to both the customer's stream and the warehouse's: the shopper's history is incomplete
/// without it, and so is the warehouse's day. Nothing in the framework couples the two — each
/// append names the stream it is for.
/// </remarks>
[EventType("OrderDespatched")]
public record OrderDespatchedEvent(
    string OrderId,
    string WarehouseCode,
    string Carrier,
    string TrackingReference) : IEvent;

/// <summary>
/// The carrier says the parcel arrived.
/// </summary>
[EventType("OrderDelivered")]
public record OrderDeliveredEvent(string OrderId, DateTimeOffset DeliveredOn) : IEvent;

/// <summary>
/// The customer sent something back.
/// </summary>
/// <remarks>
/// A return is a new fact, not the undoing of an old one: the despatch happened and stays in the
/// log. Every model in this sample folds both and works out its own answer, which is the whole
/// reason the history is kept rather than a running total.
/// </remarks>
[EventType("OrderItemReturned")]
public record OrderItemReturnedEvent(string OrderId, string Sku, int Quantity, decimal Refund) : IEvent;
