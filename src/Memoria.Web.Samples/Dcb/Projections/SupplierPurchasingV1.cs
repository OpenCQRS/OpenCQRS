using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// A supplier's purchasing at a glance — the shape this read model was first written in.
/// </summary>
/// <remarks>
/// <para>
/// Version 1 of <c>SupplierPurchasing</c>, kept beside the version 2 in
/// <see cref="SupplierPurchasing"/> so the pair reads as a model evolving rather than a name
/// reused. It tracks only how many orders have been raised with the supplier; version 2 added the
/// committed value and the per-order status, and the version is what keeps a snapshot of this
/// shape from being read back as the newer one.
/// </para>
/// <para>
/// Its boundary is <c>supplier:{id}</c>, the second tag every purchasing event carries, exactly as
/// version 2's is.
/// </para>
/// </remarks>
[ProjectionType("SupplierPurchasing", 1)]
public class SupplierPurchasingV1 : DcbProjection
{
    public string SupplierId { get; private set; } = string.Empty;

    /// <summary>
    /// How many orders have been raised with the supplier. The one figure this version kept.
    /// </summary>
    public int OrdersRaised { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(PurchaseOrderRaisedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            // The boundary is one supplier, so everything that reaches this fold is already theirs.
            case PurchaseOrderRaisedEvent raised:
                SupplierId = raised.SupplierId;
                OrdersRaised++;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the version 1 page, and carries the boundary it is folded from: one supplier, holding
/// every purchase order raised with them.
/// </summary>
public class SupplierPurchasingV1Id(string supplierId) : IDcbProjectionId<SupplierPurchasingV1>
{
    public string Id { get; } = supplierId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("supplier", supplierId));
}
