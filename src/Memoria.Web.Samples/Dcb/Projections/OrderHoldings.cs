using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// What one order is holding: the stock set aside for it, and the stock that has since left.
/// </summary>
/// <remarks>
/// <para>
/// Folded from <c>order:{id}</c>, a tag no write model in this sample is identified by. A read model
/// is free to draw its boundary wherever the question is, because it decides nothing and so contends
/// with nobody.
/// </para>
/// <para>
/// This is the order seen from the warehouse's side, and it is worth holding it next to
/// <c>Streamed/Projections/OrderSummary</c>, which is the same order seen from the shopper's. One is
/// folded from tags, the other from a stream; the choice was made per decision, not per application,
/// and the two models sit side by side without knowing about each other.
/// </para>
/// </remarks>
[ProjectionType("OrderHoldings")]
public class OrderHoldings : DcbProjection
{
    /// <summary>
    /// How much of each product is reserved for this order right now.
    /// </summary>
    /// <remarks>
    /// A private setter over a whole new dictionary each time, rather than a mutable one that is
    /// added to. A projection is stored and read back, so whatever holds its state has to survive
    /// the round trip: the serializer writes every public getter but can only restore what it can
    /// set. The decision models in this sample never notice, because they are folded fresh every
    /// time and never stored.
    /// </remarks>
    public IReadOnlyDictionary<string, int> Reserved { get; private set; } =
        new Dictionary<string, int>();

    /// <summary>
    /// How much of each product has actually been picked for this order.
    /// </summary>
    public IReadOnlyDictionary<string, int> Picked { get; private set; } =
        new Dictionary<string, int>();

    public int LinesReserved => Reserved.Count;

    public int UnitsReserved => Reserved.Values.Sum();

    public int UnitsPicked => Picked.Values.Sum();

    public bool FullyPicked => Reserved.Count == 0 && Picked.Count > 0;

    /// <summary>
    /// The catalogue events are outside this filter, and would be outside the boundary anyway: a
    /// product is created under <c>product:</c> and <c>sku:</c>, never under an order's tag.
    /// </summary>
    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(StockReservedEvent),
        typeof(StockReservationReleasedEvent),
        typeof(StockPickedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case StockReservedEvent reserved:
                Reserved = With(Reserved, reserved.ProductId, reserved.Quantity);
                return true;

            case StockReservationReleasedEvent released:
                Reserved = With(Reserved, released.ProductId, -released.Quantity);
                return true;

            case StockPickedEvent picked:
                Reserved = With(Reserved, picked.ProductId, -picked.Quantity);
                Picked = With(Picked, picked.ProductId, picked.Quantity);
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> quantities, string productId, int quantity)
    {
        var updated = new Dictionary<string, int>(quantities);
        var total = updated.GetValueOrDefault(productId) + quantity;

        if (total > 0)
        {
            updated[productId] = total;
        }
        else
        {
            updated.Remove(productId);
        }

        return updated;
    }
}

/// <summary>
/// Identifies the holdings, and therefore their snapshot, and carries the boundary they are folded
/// from.
/// </summary>
public class OrderHoldingsId(string orderId) : IDcbProjectionId<OrderHoldings>
{
    public string Id { get; } = orderId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("order", orderId));
}
