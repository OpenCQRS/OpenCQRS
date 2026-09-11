using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Dcb.Events;

/// <summary>
/// A purchase order was raised with a supplier to restock the shelves.
/// </summary>
/// <remarks>
/// <para>
/// Appended under two tags, <c>purchase-order:{id}</c> and <c>supplier:{id}</c>, so the same fact
/// is inside the order's boundary and inside the supplier's: the order has been opened, and the
/// supplier has one more piece of business on the books. It is the purchasing counterpart of
/// <see cref="StockReservedEvent"/>, which lives inside a product's boundary and an order's at once.
/// </para>
/// <para>
/// Every event in this file carries the same pair, because a purchase order belongs to its supplier
/// for the whole of its life — which is what lets <see cref="Aggregates.PurchaseOrder"/> read one
/// order and <c>SupplierPurchasing</c> read all of a supplier's.
/// </para>
/// </remarks>
[EventType("PurchaseOrderRaised")]
public record PurchaseOrderRaisedEvent(
    string PurchaseOrderId,
    string SupplierId,
    DateTimeOffset RaisedOn) : IEvent;

/// <summary>
/// A line was added to a purchase order before it was approved.
/// </summary>
[EventType("PurchaseOrderLineAdded")]
public record PurchaseOrderLineAddedEvent(
    string PurchaseOrderId,
    string ProductId,
    int Quantity,
    decimal UnitCost) : IEvent;

/// <summary>
/// A purchase order was approved and sent to the supplier.
/// </summary>
/// <remarks>
/// Version 2 of this event. The first version recorded only that approval had happened; a later
/// change to the sample decided that who approved it, and when, is worth keeping, and gave the new
/// shape a version of its own rather than reinterpreting the old one. A store still holding the
/// first would deserialize both side by side.
/// </remarks>
[EventType("PurchaseOrderApproved", 2)]
public record PurchaseOrderApprovedEvent(
    string PurchaseOrderId,
    string ApprovedBy,
    DateTimeOffset ApprovedOn) : IEvent;

/// <summary>
/// Goods against a purchase order arrived at the warehouse.
/// </summary>
/// <remarks>
/// A new fact rather than an edit of the line it fulfils: a part delivery and the line it came
/// against both stay in the log, so a model can tell what was ordered from what has turned up.
/// </remarks>
[EventType("PurchaseOrderReceived")]
public record PurchaseOrderReceivedEvent(
    string PurchaseOrderId,
    string ProductId,
    int Quantity) : IEvent;

/// <summary>
/// A purchase order was cancelled before it was fulfilled.
/// </summary>
[EventType("PurchaseOrderCancelled")]
public record PurchaseOrderCancelledEvent(string PurchaseOrderId, string Reason) : IEvent;
