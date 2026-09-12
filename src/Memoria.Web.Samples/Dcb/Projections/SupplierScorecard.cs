using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// A supplier's scorecard: how many of their orders were scored, and how well.
/// </summary>
/// <remarks>
/// <para>
/// Retired with the rating scheme, for the reason <see cref="Aggregates.SupplierRating"/> is: the
/// snapshots it wrote are still in the store, and only its own shape can read them back.
/// </para>
/// <para>
/// It folds a live event as well as a retired one. Every order raised is a line on the card,
/// scored or not, so an order raised since the scheme was dropped still counts as one nobody
/// scored — which is what a buyer opening an old scorecard would expect to see. A retired model is
/// not cut off from the log; it is only no longer written to.
/// </para>
/// </remarks>
[Obsolete("Supplier ratings were dropped. Kept so the scorecards already snapshotted still read.")]
[ProjectionType("SupplierScorecard")]
public class SupplierScorecard : DcbProjection
{
    public string SupplierId { get; private set; } = string.Empty;

    public int OrdersRaised { get; private set; }

    public int OrdersScored { get; private set; }

    public int TotalScore { get; private set; }

    public decimal AverageScore => OrdersScored == 0 ? 0 : Math.Round((decimal)TotalScore / OrdersScored, 1);

    public int BestScore { get; private set; }

    public int WorstScore { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(PurchaseOrderRaisedEvent),
        typeof(SupplierRatedEvent)
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

            case SupplierRatedEvent rated:
                SupplierId = rated.SupplierId;
                OrdersScored++;
                TotalScore += rated.Score;
                BestScore = OrdersScored == 1 ? rated.Score : Math.Max(BestScore, rated.Score);
                WorstScore = OrdersScored == 1 ? rated.Score : Math.Min(WorstScore, rated.Score);
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the scorecard, and therefore its snapshot, and carries the boundary it is folded
/// from: one supplier.
/// </summary>
/// <remarks>
/// Prefixed, where <see cref="SupplierPurchasingId"/> is the bare supplier id. A projection snapshot
/// is identified by <c>{Id}:{type version}</c> and a digest of its boundary, and the model type is
/// in neither: under the bare id this would be the same row as
/// <see cref="SupplierPurchasingV1Id"/> — same id, same version, same boundary — and each refresh
/// would overwrite the other's.
/// </remarks>
[Obsolete("Addresses a SupplierScorecard, which is retired with the scheme it belonged to.")]
public class SupplierScorecardId(string supplierId) : IDcbProjectionId<SupplierScorecard>
{
    public string Id { get; } = $"scorecard-{supplierId}";

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("supplier", supplierId));
}
