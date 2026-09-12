using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Events;

/// <summary>
/// A customer earned loyalty points for an order, back when the shop ran a loyalty scheme.
/// </summary>
/// <remarks>
/// <para>
/// Obsolete because the scheme closed, not because the shape changed: no later version replaces
/// it, nothing raises it any more, and the type stays only so the points already in the customers'
/// streams still read. That is what sets a retired event apart from an old one like
/// <see cref="ProductReviewEditedEventV1"/>, which has a version 2 after it.
/// </para>
/// <para>
/// It carries <c>CustomerId</c> because the customer's stream holds it beside the orders, and
/// <c>OrderId</c> because the points were earned for one of them — so a model about a single order
/// could narrow the stream to it by property, the way <see cref="Aggregates.OrderId"/> does.
/// </para>
/// </remarks>
[Obsolete("The loyalty scheme has closed. Kept so the points already in the log still read; nothing raises it any more.")]
[EventType("LoyaltyPointsEarned")]
public record LoyaltyPointsEarnedEvent(
    string CustomerId,
    string OrderId,
    int Points,
    DateTimeOffset EarnedOn) : IEvent;
