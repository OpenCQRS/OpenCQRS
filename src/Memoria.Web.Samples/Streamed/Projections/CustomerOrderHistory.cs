using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Projections;

/// <summary>
/// Everything the customer has ever ordered, as the account page shows it.
/// </summary>
/// <remarks>
/// <para>
/// Folded from the whole stream, so it sees the orders <see cref="OrderSummary"/> only ever sees one
/// of. That is the trade the pair of them makes: one snapshot per order that a list has to gather
/// up, against one snapshot per customer that a single order page would over-read.
/// </para>
/// <para>
/// It decides nothing, which is why it is free to be this wide. A write model reading every order a
/// customer ever placed would serialise the lot; a read model contends with nobody.
/// </para>
/// </remarks>
[ProjectionType("CustomerOrderHistory")]
public class CustomerOrderHistory : Projection
{
    public string CustomerId { get; private set; } = string.Empty;

    public int OrdersPlaced { get; private set; }

    public int OrdersDelivered { get; private set; }

    public int OrdersCancelled { get; private set; }

    public decimal LifetimeSpend { get; private set; }

    public decimal Refunded { get; private set; }

    public DateTimeOffset? FirstOrderOn { get; private set; }

    public DateTimeOffset? LastOrderOn { get; private set; }

    /// <summary>
    /// The orders still on their way, most recent first.
    /// </summary>
    public IReadOnlyList<string> OpenOrders { get; private set; } = [];

    /// <summary>
    /// How many of each SKU the customer has bought and kept, which is what the "buy it again"
    /// strip on the account page is built from.
    /// </summary>
    public IReadOnlyDictionary<string, int> Bought { get; private set; } =
        new Dictionary<string, int>();

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(OrderPlacedEvent),
        typeof(OrderItemAddedEvent),
        typeof(OrderItemRemovedEvent),
        typeof(OrderPaidEvent),
        typeof(OrderCancelledEvent),
        typeof(OrderDeliveredEvent),
        typeof(OrderItemReturnedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                CustomerId = placed.CustomerId;
                OrdersPlaced++;
                FirstOrderOn ??= placed.PlacedOn;
                LastOrderOn = placed.PlacedOn;
                OpenOrders = [placed.OrderId, ..OpenOrders];
                return true;

            case OrderItemAddedEvent added:
                Bought = With(Bought, added.Sku, added.Quantity);
                return true;

            case OrderItemRemovedEvent removed:
                Bought = With(Bought, removed.Sku, -removed.Quantity);
                return true;

            case OrderPaidEvent paid:
                LifetimeSpend += paid.Amount;
                return true;

            case OrderCancelledEvent cancelled:
                OrdersCancelled++;
                OpenOrders = Without(OpenOrders, cancelled.OrderId);
                return true;

            case OrderDeliveredEvent delivered:
                OrdersDelivered++;
                OpenOrders = Without(OpenOrders, delivered.OrderId);
                return true;

            case OrderItemReturnedEvent returned:
                Bought = With(Bought, returned.Sku, -returned.Quantity);
                Refunded += returned.Refund;
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyList<string> Without(IReadOnlyList<string> orders, string orderId) =>
        [..orders.Where(candidate => candidate != orderId)];

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> quantities, string sku, int quantity)
    {
        var updated = new Dictionary<string, int>(quantities);
        var total = updated.GetValueOrDefault(sku) + quantity;

        if (total > 0)
        {
            updated[sku] = total;
        }
        else
        {
            updated.Remove(sku);
        }

        return updated;
    }
}

/// <summary>
/// Identifies the history, and therefore its snapshot. One per customer, and so one per stream.
/// </summary>
/// <remarks>
/// No event property filter, because the question is about all of them. Compare
/// <see cref="OrderSummaryId"/>, which narrows the same stream to a single order.
/// </remarks>
public class CustomerOrderHistoryId(string customerId) : IProjectionId<CustomerOrderHistory>
{
    public string Id { get; } = customerId;

    public IDictionary<string, string>? EventPropertyFilter => null;
}
