using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Aggregates;

/// <summary>
/// How a supplier has been scored, folded from the events tagged with them.
/// </summary>
/// <remarks>
/// <para>
/// Retired with the rating scheme. It stays because the snapshots it wrote are still in the store,
/// and a snapshot can only be read back by the shape that wrote it: remove the type and every one
/// of those rows becomes something nothing can name. Obsolete says the rest — nothing new should
/// go through <see cref="Rate"/>, and the compiler says so wherever something tries.
/// </para>
/// <para>
/// A DCB write model whose boundary is a single tag, <c>supplier:{id}</c>, the tag every purchasing
/// event carries second. It reads only the one event the scheme produced, so the purchase orders
/// inside the same boundary never reach its fold — the same one-tag-two-models arrangement
/// <see cref="StockLevel"/> and <see cref="Product"/> have over a product.
/// </para>
/// </remarks>
[Obsolete("Supplier ratings were dropped. Kept so the ratings already snapshotted still read; do not score through it.")]
[AggregateType("SupplierRating")]
public class SupplierRating : DcbAggregateRoot
{
    public string SupplierId { get; private set; } = string.Empty;

    public int Ratings { get; private set; }

    public int TotalScore { get; private set; }

    public decimal AverageScore => Ratings == 0 ? 0 : Math.Round((decimal)TotalScore / Ratings, 1);

    /// <summary>
    /// The orders already scored. The scheme took one score per order, and this is what
    /// <see cref="Rate"/> refuses a second score against.
    /// </summary>
    public IReadOnlyList<string> RatedOrders { get; private set; } = [];

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(SupplierRatedEvent)
    ];

    /// <summary>
    /// The supplier this fold is about, taken from the boundary. One tag, because the boundary is
    /// one supplier.
    /// </summary>
    private string SupplierCode => Tags.Single(tag => tag.Key == "supplier").Value;

    /// <summary>
    /// Scores the supplier on one order, or explains why the score cannot be taken.
    /// </summary>
    public string? Rate(string purchaseOrderId, int score, string ratedBy)
    {
        if (score is < 1 or > 5) return "A score has to be between one and five.";
        if (RatedOrders.Contains(purchaseOrderId)) return $"Purchase order '{purchaseOrderId}' has already been scored.";

        // Staged with no tags of its own, so it inherits the aggregate's — which the store set from
        // the boundary. For a model that reads exactly one tag that is exactly the right tag.
        Add(new SupplierRatedEvent(SupplierCode, purchaseOrderId, score, ratedBy));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case SupplierRatedEvent rated:
                SupplierId = rated.SupplierId;
                Ratings++;
                TotalScore += rated.Score;
                RatedOrders = [..RatedOrders, rated.PurchaseOrderId];
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the rating, and therefore its snapshot, and carries the boundary it is folded from:
/// one supplier.
/// </summary>
[Obsolete("Addresses a SupplierRating, which is retired with the scheme it belonged to.")]
public class SupplierRatingId(string supplierId) : IDcbAggregateId<SupplierRating>
{
    public string Id { get; } = supplierId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("supplier", supplierId));
}
