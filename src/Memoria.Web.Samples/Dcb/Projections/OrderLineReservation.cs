using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Aggregates;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// One order's standing on one product — the answer to "is this order holding any of that?".
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to <see cref="StockReservationDecision"/>, and the reason a boundary comes in two
/// shapes. That model asks a question about a product <em>and</em> an order, so its boundary is the
/// union <c>product:p1 OR order:o7</c> and it folds every reservation of the product and every line
/// on the order. This one asks a question about the pair, so its boundary is the intersection
/// <c>product:p1 AND order:o7</c> and it folds only the events concerning both — in a shop of any
/// size, a handful rather than thousands.
/// </para>
/// <para>
/// Notice what the narrower boundary removes from the fold. <see cref="StockReservationDecision"/>
/// guards nearly every case on <c>== ProductId</c> or <c>== OrderId</c>, because its union brings in
/// other orders' reservations of this product and this order's holdings of other products, and the
/// model has to sort them out itself. Here the boundary has already done that: every event that
/// reaches <see cref="Apply{T}"/> concerns this order and this product, so there is nothing to check.
/// </para>
/// <para>
/// It follows that an intersection is only as good as the tagging. A reservation is appended under
/// both tags, so it lands here; <see cref="ProductCreatedEvent"/> is appended under the product and
/// its SKU alone, and <see cref="StockReplenishedEvent"/> under the product alone, so neither is
/// inside this boundary however much it might seem to concern the pair. That is the right answer for
/// this question and the wrong one for the reservation rule — which is exactly why that rule reads
/// the union.
/// </para>
/// </remarks>
[ProjectionType("OrderLineReservation")]
public class OrderLineReservation : DcbProjection
{
    /// <summary>How much of this product the order is holding right now.</summary>
    public int Held { get; private set; }

    /// <summary>How much of it has been picked.</summary>
    public int Picked { get; private set; }

    /// <summary>
    /// How many times this order has reserved this product, so a re-reservation after a release is
    /// visible rather than implied.
    /// </summary>
    public int TimesReserved { get; private set; }

    public bool IsHolding => Held > 0;

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
            // No `when reserved.OrderId == ...` guard. The boundary is the guard: an event only
            // reaches this fold if it carries both tags.
            case StockReservedEvent reserved:
                Held += reserved.Quantity;
                TimesReserved++;
                return true;

            case StockReservationReleasedEvent released:
                Held -= released.Quantity;
                return true;

            case StockPickedEvent picked:
                Held -= picked.Quantity;
                Picked += picked.Quantity;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the line, and carries the intersection boundary it is folded from.
/// </summary>
/// <remarks>
/// The boundary reads "events carrying <c>product:{productId}</c> <em>and</em> <c>order:{orderId}</c>",
/// where <see cref="StockReservationDecisionId"/> reads "<em>or</em>".
/// </remarks>
public class OrderLineReservationId(string productId, string orderId)
    : IDcbProjectionId<OrderLineReservation>
{
    public string Id { get; } = $"{productId}-{orderId}";

    public TagQuery Boundary { get; } =
        TagQuery.AllOf(new Tag("product", productId), new Tag("order", orderId));
}
