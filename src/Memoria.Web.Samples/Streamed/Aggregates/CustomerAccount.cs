using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Aggregates;

/// <summary>
/// What the customer owes and what they are trusted with, folded from the whole of their stream.
/// </summary>
/// <remarks>
/// <para>
/// The second write model over <see cref="Streams.CustomerStreamId"/>, and the reason that stream
/// is the customer's rather than the order's. Whether another order may be placed on account is a
/// question about every order at once: no single <see cref="Order"/> can answer it, and asking a
/// read model would mean deciding on state that may already be stale.
/// </para>
/// <para>
/// Its identifier carries no event property filter, so it reads every event in the stream — the
/// opposite choice from <see cref="OrderId"/>, made in the same place for the same reason.
/// </para>
/// </remarks>
[AggregateType("CustomerAccount")]
public class CustomerAccount : AggregateRoot
{
    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>
    /// How much the customer may have unpaid at once. A standing figure rather than an event of its
    /// own in this sample; a real one would raise and lower it and fold that too.
    /// </summary>
    public decimal CreditLimit { get; private set; } = 500m;

    /// <summary>Placed but not yet paid for.</summary>
    public decimal Outstanding { get; private set; }

    /// <summary>Paid, less anything refunded.</summary>
    public decimal LifetimeSpend { get; private set; }

    public int OrdersPlaced { get; private set; }

    public int OrdersCancelled { get; private set; }

    public decimal CreditAvailable => CreditLimit - Outstanding;

    /// <summary>
    /// Deliberately narrower than <see cref="Order.EventTypeFilter"/>: despatch and delivery move an
    /// order along without changing what is owed, so this fold never reads them.
    /// </summary>
    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(OrderPlacedEvent),
        typeof(OrderItemAddedEvent),
        typeof(OrderItemRemovedEvent),
        typeof(OrderPaidEvent),
        typeof(OrderCancelledEvent),
        typeof(OrderItemReturnedEvent)
    ];

    /// <summary>
    /// Says whether an order of this size may go on account, and why not when it may not.
    /// </summary>
    /// <remarks>
    /// A question rather than a decision: nothing is staged, because the answer is only good for as
    /// long as the version this was folded at. The caller appends whatever it decides with that
    /// version as its expected version, which is what makes the answer hold.
    /// </remarks>
    public string? CheckCreditFor(decimal amount)
    {
        if (amount <= 0) return "An order has to be worth something.";

        return amount > CreditAvailable
            ? $"That would put {CustomerId} {amount - CreditAvailable:0.00} over a credit limit of {CreditLimit:0.00}."
            : null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                CustomerId = placed.CustomerId;
                OrdersPlaced++;
                return true;

            case OrderItemAddedEvent added:
                Outstanding += added.Quantity * added.UnitPrice;
                return true;

            case OrderItemRemovedEvent removed:
                Outstanding -= removed.Quantity * removed.UnitPrice;
                return true;

            case OrderPaidEvent paid:
                Outstanding -= paid.Amount;
                LifetimeSpend += paid.Amount;
                return true;

            case OrderCancelledEvent:
                OrdersCancelled++;
                return true;

            case OrderItemReturnedEvent returned:
                LifetimeSpend -= returned.Refund;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the account, which is the customer, which is the stream.
/// </summary>
/// <remarks>
/// No event property filter: this model wants everything in the stream, and saying so is simply
/// returning null. Compare <see cref="OrderId"/>, which narrows the same stream to one order.
/// </remarks>
public class CustomerAccountId(string customerId) : IAggregateId<CustomerAccount>
{
    public string Id { get; } = customerId;

    public IDictionary<string, string>? EventPropertyFilter => null;
}
