using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Aggregates;

/// <summary>
/// The points a customer had earned under the loyalty scheme, folded from the whole of their
/// stream.
/// </summary>
/// <remarks>
/// <para>
/// Retired with the scheme. It stays because the snapshots it wrote are still in the store, and a
/// snapshot can only be read back by the shape that wrote it: remove the type and every one of
/// those rows becomes something nothing can name. Obsolete says the rest — nothing new should go
/// through <see cref="Earn"/>, and the compiler says so wherever something tries.
/// </para>
/// <para>
/// The third write model over <see cref="Streams.CustomerStreamId"/>, beside
/// <see cref="CustomerAccount"/> and <see cref="Order"/>. It reads the one event the scheme
/// produced, so nothing the shop still does reaches its fold.
/// </para>
/// </remarks>
[Obsolete("The loyalty scheme has closed. Kept so the balances already snapshotted still read; do not earn points through it.")]
[AggregateType("LoyaltyBalance")]
public class LoyaltyBalance : AggregateRoot
{
    public string CustomerId { get; private set; } = string.Empty;

    public int Points { get; private set; }

    public int TimesEarned { get; private set; }

    public DateTimeOffset? LastEarnedOn { get; private set; }

    /// <summary>
    /// The orders points were earned for. The scheme awarded once per order, and this is what
    /// <see cref="Earn"/> refuses a second award against.
    /// </summary>
    public IReadOnlyList<string> RewardedOrders { get; private set; } = [];

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(LoyaltyPointsEarnedEvent)
    ];

    /// <summary>
    /// Awards points for an order, or explains why they cannot be awarded.
    /// </summary>
    public string? Earn(string customerId, string orderId, int points, DateTimeOffset earnedOn)
    {
        if (points < 1) return $"An award has to be at least one point, not {points}.";
        if (RewardedOrders.Contains(orderId)) return $"Order '{orderId}' has already earned its points.";

        Add(new LoyaltyPointsEarnedEvent(customerId, orderId, points, earnedOn));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case LoyaltyPointsEarnedEvent earned:
                CustomerId = earned.CustomerId;
                Points += earned.Points;
                TimesEarned++;
                LastEarnedOn = earned.EarnedOn;
                RewardedOrders = [..RewardedOrders, earned.OrderId];
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the balance, and therefore its snapshot. One per customer, reading the whole stream.
/// </summary>
/// <remarks>
/// Prefixed, where <see cref="CustomerAccountId"/> is the bare customer id: a snapshot is keyed by
/// <c>{Id}:{type version}</c> with the model type in neither half, so two write models over the same
/// stream under the same id would be the same row. The same reason <c>StockLevelId</c> is prefixed
/// on the DCB side.
/// </remarks>
[Obsolete("Addresses a LoyaltyBalance, which is retired with the scheme it belonged to.")]
public class LoyaltyBalanceId(string customerId) : IAggregateId<LoyaltyBalance>
{
    public string Id { get; } = $"loyalty-{customerId}";

    public IDictionary<string, string>? EventPropertyFilter => null;
}
