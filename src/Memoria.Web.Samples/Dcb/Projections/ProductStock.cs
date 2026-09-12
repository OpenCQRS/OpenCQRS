using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Aggregates;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Projections;

/// <summary>
/// One product as the catalogue page shows it: what it is called, what it costs, and whether anyone
/// can have one.
/// </summary>
/// <remarks>
/// <para>
/// A projection differs from an aggregate in one way only — it never produces events, so it has no
/// <c>Add</c> and no uncommitted events. Everything else is the same fold.
/// </para>
/// <para>
/// Its boundary is <c>product:{id}</c>, the same tag <see cref="Product"/> and
/// <see cref="StockLevel"/> read, but it reads both of their event types rather than either's. The
/// two write models keep their filters narrow so that a decision folds as little as possible; this
/// one is answering a question, not making a decision, so it takes the lot and puts it on one page.
/// </para>
/// </remarks>
[ProjectionType("ProductStock")]
public class ProductStock : DcbProjection
{
    public string Name { get; private set; } = "unknown";

    public string Sku { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool InCatalogue { get; private set; }

    public bool Discontinued { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available => OnHand - Reserved;

    public bool CanBeBought => InCatalogue && !Discontinued && Available > 0;

    /// <summary>
    /// How many times this product has been reserved, which is the closest thing the log has to a
    /// measure of demand.
    /// </summary>
    public int TimesReserved { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductCreatedEvent),
        typeof(ProductDetailsChangedEvent),
        typeof(ProductDiscontinuedEvent),
        typeof(ProductDeletedEvent),
        typeof(StockReplenishedEvent),
        typeof(StockReservedEvent),
        typeof(StockReservationReleasedEvent),
        typeof(StockPickedEvent),
        typeof(StockAdjustedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            // No `when created.ProductId == ...` guard, unlike StockReservationDecision. The
            // boundary is one product, so everything that reaches this fold is already about it.
            case ProductCreatedEvent created:
                InCatalogue = true;
                Discontinued = false;
                Name = created.Name;
                Sku = created.Sku;
                Price = created.Price;
                return true;

            case ProductDetailsChangedEvent changed:
                Name = changed.Name;
                Price = changed.Price;
                return true;

            case ProductDiscontinuedEvent:
                Discontinued = true;
                return true;

            case ProductDeletedEvent:
                InCatalogue = false;
                return true;

            case StockReplenishedEvent replenished:
                OnHand += replenished.Quantity;
                return true;

            case StockReservedEvent reserved:
                Reserved += reserved.Quantity;
                TimesReserved++;
                return true;

            case StockReservationReleasedEvent released:
                Reserved -= released.Quantity;
                return true;

            case StockPickedEvent picked:
                Reserved -= picked.Quantity;
                OnHand -= picked.Quantity;
                return true;

            case StockAdjustedEvent adjusted:
                OnHand += adjusted.Difference;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the page, and therefore its snapshot, and carries the boundary it is folded from.
/// </summary>
public class ProductStockId(string productId) : IDcbProjectionId<ProductStock>
{
    public string Id { get; } = productId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("product", productId));
}
