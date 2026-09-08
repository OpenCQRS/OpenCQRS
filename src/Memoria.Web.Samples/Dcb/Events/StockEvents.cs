using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Dcb.Events;

/// <summary>
/// Stock arrived for a product.
/// </summary>
/// <remarks>
/// Tagged with the product alone. Nobody's order is involved in a delivery.
/// </remarks>
[EventType("StockReplenished")]
public record StockReplenishedEvent(string ProductId, int Quantity, string GoodsInReference) : IEvent;

/// <summary>
/// Stock was set aside for an order.
/// </summary>
/// <remarks>
/// <para>
/// The event that makes this side of the sample worth having. It is appended under both
/// <c>product:{id}</c> and <c>order:{id}</c>, so it is inside the product's boundary and inside the
/// order's, and the same fact means something different seen through each: the product has fewer
/// units to give, the order has one more thing it is holding.
/// </para>
/// <para>
/// A stream could hold one of those two readings, not both. Choosing the product's stream would
/// serialise every order that touches a popular line; choosing the order's would leave nothing able
/// to see the stock. That is the case for a boundary drawn per decision rather than per entity.
/// </para>
/// </remarks>
[EventType("StockReserved")]
public record StockReservedEvent(string ProductId, string OrderId, int Quantity) : IEvent;

/// <summary>
/// A reservation was given back, because the order was cancelled or it timed out.
/// </summary>
/// <remarks>
/// Tagged like the reservation it undoes, under both the product and the order — anything that can
/// see the reservation has to be able to see it released, or it will go on counting stock that is
/// free again.
/// </remarks>
[EventType("StockReservationReleased")]
public record StockReservationReleasedEvent(string ProductId, string OrderId, int Quantity) : IEvent;

/// <summary>
/// Reserved stock left the building, so it is gone rather than merely spoken for.
/// </summary>
[EventType("StockPicked")]
public record StockPickedEvent(string ProductId, string OrderId, int Quantity) : IEvent;

/// <summary>
/// A count found less on the shelf than the log says there should be.
/// </summary>
/// <remarks>
/// A correction is a new fact, not an edit: the deliveries and the picks stay exactly as they were
/// recorded, and the discrepancy sits alongside them where someone can ask about it.
/// </remarks>
[EventType("StockAdjusted")]
public record StockAdjustedEvent(string ProductId, int Difference, string Reason) : IEvent;
