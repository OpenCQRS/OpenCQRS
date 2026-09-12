using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Projections;

/// <summary>
/// The customer's loyalty statement: what each order earned them, and how many earned nothing.
/// </summary>
/// <remarks>
/// <para>
/// Retired with the scheme, for the reason <see cref="Aggregates.LoyaltyBalance"/> is: the
/// snapshots it wrote are still in the store, and only its own shape can read them back.
/// </para>
/// <para>
/// It folds a live event as well as a retired one. Every order placed is a line on the statement,
/// with points against it or not, so an order placed since the scheme closed still counts as one
/// that earned nothing — which is what a customer opening an old statement would expect to see. A
/// retired model is not cut off from the log; it is only no longer written to.
/// </para>
/// </remarks>
[Obsolete("The loyalty scheme has closed. Kept so the statements already snapshotted still read.")]
[ProjectionType("LoyaltyStatement")]
public class LoyaltyStatement : Projection
{
    public string CustomerId { get; private set; } = string.Empty;

    public int OrdersPlaced { get; private set; }

    public int OrdersRewarded => PointsByOrder.Count;

    public int PointsEarned { get; private set; }

    public DateTimeOffset? LastEarnedOn { get; private set; }

    /// <summary>
    /// The points each order earned, keyed by order id. Replaced whole on every fold, so the
    /// snapshot round trips without a mutable field to restore.
    /// </summary>
    public IReadOnlyDictionary<string, int> PointsByOrder { get; private set; } =
        new Dictionary<string, int>();

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(OrderPlacedEvent),
        typeof(LoyaltyPointsEarnedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                CustomerId = placed.CustomerId;
                OrdersPlaced++;
                return true;

            case LoyaltyPointsEarnedEvent earned:
                CustomerId = earned.CustomerId;
                PointsEarned += earned.Points;
                LastEarnedOn = earned.EarnedOn;
                PointsByOrder = With(PointsByOrder, earned.OrderId, earned.Points);
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> points, string orderId, int earned)
    {
        var updated = new Dictionary<string, int>(points);
        updated[orderId] = updated.GetValueOrDefault(orderId) + earned;
        return updated;
    }
}

/// <summary>
/// Identifies the statement, and therefore its snapshot. One per customer, and so one per stream.
/// </summary>
/// <remarks>
/// Prefixed, where <see cref="CustomerOrderHistoryId"/> is the bare customer id, for the reason
/// <see cref="Aggregates.LoyaltyBalanceId"/> is: a snapshot is keyed by <c>{Id}:{type version}</c>
/// and the model type is in neither half.
/// </remarks>
[Obsolete("Addresses a LoyaltyStatement, which is retired with the scheme it belonged to.")]
public class LoyaltyStatementId(string customerId) : IProjectionId<LoyaltyStatement>
{
    public string Id { get; } = $"loyalty-{customerId}";

    public IDictionary<string, string>? EventPropertyFilter => null;
}
