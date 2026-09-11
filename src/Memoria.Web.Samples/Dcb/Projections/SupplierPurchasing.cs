using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Aggregates;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// A supplier's purchasing at a glance: how many orders are open with them, how many have been
/// received, and what has been committed to them.
/// </summary>
/// <remarks>
/// <para>
/// A read model whose boundary is <c>supplier:{id}</c>, the second tag every purchasing event
/// carries. Where <see cref="PurchaseOrder"/> folds one order from the order's tag, this folds all
/// of a supplier's from the supplier's — the same one-fact-two-boundaries arrangement the stock and
/// reservation models are built on, seen from the wider side.
/// </para>
/// <para>
/// Version 2 of the projection. An earlier version tracked only the count of orders; the committed
/// value and the per-order status were added later, and the type carries a version so a snapshot
/// written by the old shape is not read back as the new one.
/// </para>
/// </remarks>
[ProjectionType("SupplierPurchasing", 2)]
public class SupplierPurchasing : DcbProjection
{
    public string SupplierId { get; private set; } = string.Empty;

    /// <summary>
    /// The state each of the supplier's orders is in, keyed by purchase order id. Replaced whole on
    /// every fold, so the snapshot round trips without a mutable field to restore.
    /// </summary>
    public IReadOnlyDictionary<string, PurchaseOrderStatus> Orders { get; private set; } =
        new Dictionary<string, PurchaseOrderStatus>();

    /// <summary>
    /// What has been committed to the supplier across every order still standing, at the line costs
    /// they were raised with.
    /// </summary>
    public decimal Committed { get; private set; }

    public int OrdersRaised => Orders.Count;

    public int OrdersApproved => Orders.Values.Count(status => status == PurchaseOrderStatus.Approved);

    public int OrdersReceived => Orders.Values.Count(status => status == PurchaseOrderStatus.Received);

    public int OrdersCancelled => Orders.Values.Count(status => status == PurchaseOrderStatus.Cancelled);

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(PurchaseOrderRaisedEvent),
        typeof(PurchaseOrderLineAddedEvent),
        typeof(PurchaseOrderApprovedEvent),
        typeof(PurchaseOrderReceivedEvent),
        typeof(PurchaseOrderCancelledEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            // The boundary is one supplier, so everything that reaches this fold is already theirs.
            case PurchaseOrderRaisedEvent raised:
                SupplierId = raised.SupplierId;
                Orders = With(Orders, raised.PurchaseOrderId, PurchaseOrderStatus.Draft);
                return true;

            case PurchaseOrderLineAddedEvent added:
                Committed += added.Quantity * added.UnitCost;
                return true;

            case PurchaseOrderApprovedEvent approved:
                Orders = With(Orders, approved.PurchaseOrderId, PurchaseOrderStatus.Approved);
                return true;

            case PurchaseOrderReceivedEvent:
                return true;

            case PurchaseOrderCancelledEvent cancelled:
                Orders = With(Orders, cancelled.PurchaseOrderId, PurchaseOrderStatus.Cancelled);
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, PurchaseOrderStatus> With(
        IReadOnlyDictionary<string, PurchaseOrderStatus> orders,
        string purchaseOrderId,
        PurchaseOrderStatus status)
    {
        var updated = new Dictionary<string, PurchaseOrderStatus>(orders) { [purchaseOrderId] = status };
        return updated;
    }
}

/// <summary>
/// Identifies the page, and therefore its snapshot, and carries the boundary it is folded from: one
/// supplier, holding every purchase order raised with them.
/// </summary>
public class SupplierPurchasingId(string supplierId) : IDcbProjectionId<SupplierPurchasing>
{
    public string Id { get; } = supplierId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("supplier", supplierId));
}
