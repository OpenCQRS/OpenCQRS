using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Dcb.Events;

/// <summary>
/// A buyer scored a supplier on how a purchase order went, back when the shop rated its suppliers.
/// </summary>
/// <remarks>
/// <para>
/// Obsolete because the rating scheme was dropped, not because the shape changed: no later version
/// replaces it, nothing appends it any more, and the type stays only so the scores already in the
/// log still read. Compare <see cref="Projections.SupplierPurchasingV1"/> over the same boundary,
/// which is old because a version 2 followed it.
/// </para>
/// <para>
/// Tagged with the supplier alone, so it sits inside <c>supplier:{id}</c> beside the purchasing
/// events. The live models over that boundary never read it — it is outside their event type
/// filters — but it is inside the boundary all the same: a retired event does not leave the
/// boundary it was written in, and a page listing what the boundary holds will list it.
/// </para>
/// </remarks>
[Obsolete("Supplier ratings were dropped. Kept so the scores already in the log still read; nothing appends it any more.")]
[EventType("SupplierRated")]
public record SupplierRatedEvent(
    string SupplierId,
    string PurchaseOrderId,
    int Score,
    string RatedBy) : IEvent;
