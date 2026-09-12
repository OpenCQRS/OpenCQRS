using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Aggregates;

/// <summary>
/// One product in the catalogue.
/// </summary>
/// <remarks>
/// <para>
/// A DCB write model: <see cref="DcbAggregateRoot"/> is the <see cref="DcbModel"/> that can stage
/// events, so the aggregate belongs to no stream and is folded from the tags its identifier names.
/// </para>
/// <para>
/// It is folded from two different boundaries, and that is the point. Creating a product reads
/// <c>product:{id} OR sku:{code}</c>, so the fold can see whether another product already claimed
/// the code. Everything afterwards reads <c>product:{id}</c> alone, because those decisions depend
/// on nothing else.
/// </para>
/// </remarks>
[AggregateType("Product")]
public class Product : DcbAggregateRoot
{
    public bool Exists { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool Discontinued { get; private set; }

    /// <summary>
    /// How big the box is and what it weighs.
    /// </summary>
    /// <remarks>
    /// A whole value held on the model rather than four loose decimals, and it comes back off the
    /// snapshot the same way it went on: a private setter over a replacement value, never a value
    /// edited in place. <c>PrivateSetterContractResolver</c> is what lets the serializer restore it
    /// without the model having to expose a setter to the rest of the application.
    /// </remarks>
    public PackagedSize Packaging { get; private set; } = new(0, 0, 0, 0);

    /// <summary>
    /// The words the catalogue search knows this product by.
    /// </summary>
    /// <remarks>
    /// A list of plain strings, and the simplest case of the same rule: replaced whole on every
    /// fold, so nothing reading the model can catch it halfway through a change and the serializer
    /// has one value to write.
    /// </remarks>
    public IReadOnlyList<string> Keywords { get; private set; } = [];

    /// <summary>
    /// The finishes this product is sold in, and what each does to the price.
    /// </summary>
    /// <remarks>
    /// A list of values rather than of strings, which is where a snapshot earns its round trip:
    /// <see cref="ProductVariant"/> has to survive being written and read back with every field
    /// intact, or a fold from the snapshot and a fold from the events would disagree.
    /// </remarks>
    public IReadOnlyList<ProductVariant> Variants { get; private set; } = [];

    /// <summary>The cheapest a variant of this product can be had for.</summary>
    public decimal LowestVariantPrice =>
        Variants.Count == 0 ? Price : Price + Variants.Min(variant => variant.PriceDifference);

    /// <summary>
    /// Stock events are outside this filter on purpose. What a product is called and what it costs
    /// has nothing to do with how many are on the shelf, and a model that reads a tag does not have
    /// to read everything written under it — see <see cref="StockLevel"/>, which reads the same tag
    /// for the other half.
    /// </summary>
    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductCreatedEvent),
        typeof(ProductDetailsChangedEvent),
        typeof(ProductDiscontinuedEvent),
        typeof(ProductDeletedEvent)
    ];

    /// <summary>
    /// The product this fold is about, taken from the boundary rather than from a constructor.
    /// </summary>
    /// <remarks>
    /// <see cref="DcbModel.Tags"/> is how a model learns what it was built from: the store sets it
    /// from the identifier's boundary before folding, so <c>Apply</c> and the decision methods can
    /// both read it.
    /// </remarks>
    private string ProductCode => Tags.Single(tag => tag.Key == "product").Value;

    /// <summary>
    /// Creates the product, or explains why it cannot be created.
    /// </summary>
    /// <remarks>
    /// The fold covers the SKU as well as the id, so <see cref="Exists"/> being true here means
    /// either this product or its code is already in the catalogue. That is the only rule checked
    /// here, because it is the only one that needs the fold — the shape of the input is a
    /// validator's job.
    /// </remarks>
    public string? Create(
        string productId,
        string name,
        string sku,
        decimal price,
        PackagedSize packaging,
        IReadOnlyList<string> keywords,
        IReadOnlyList<ProductVariant> variants)
    {
        if (Exists) return $"A product with SKU '{sku}' already exists.";
        if (price < 0) return "A price cannot be negative.";

        // Staged with no tags of its own, so it inherits the aggregate's — which the store set from
        // the boundary, and which is exactly product:{id} and sku:{sku}.
        Add(new ProductCreatedEvent(productId, name, sku, price, packaging, keywords, variants));

        return null;
    }

    /// <summary>
    /// Changes what the product is called and what it costs, or explains why it cannot.
    /// </summary>
    /// <remarks>
    /// The SKU is not among them. It is part of the boundary a creation is folded from, so moving
    /// it would mean deciding all over again whether the new code is free — a different decision,
    /// and one this sample does not make.
    /// </remarks>
    public string? ChangeDetails(string name, decimal price)
    {
        if (!Exists) return "That product is not in the catalogue.";
        if (price < 0) return "A price cannot be negative.";
        if (name == Name && price == Price) return "Nothing has changed.";

        Add(new ProductDetailsChangedEvent(ProductCode, name, price));

        return null;
    }

    /// <summary>
    /// Takes the product off sale, or explains why it cannot be.
    /// </summary>
    public string? Discontinue(string reason)
    {
        if (!Exists) return "That product is not in the catalogue.";
        if (Discontinued) return "That product is already discontinued.";

        Add(new ProductDiscontinuedEvent(ProductCode, reason));

        return null;
    }

    /// <summary>
    /// Removes the product from the catalogue, or explains why it cannot be removed.
    /// </summary>
    /// <remarks>
    /// The tags are given explicitly because this fold's boundary may be the product alone, and the
    /// event has to be readable from two places: this product's history, and the code's. Without the
    /// SKU tag the code would stay taken forever.
    /// </remarks>
    public string? Delete()
    {
        if (!Exists) return "That product is not in the catalogue.";

        Add(new ProductDeletedEvent(ProductCode, Sku),
            new Tag("product", ProductCode), new Tag("sku", Sku));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case ProductCreatedEvent created:
                Exists = true;
                Discontinued = false;
                Name = created.Name;
                Sku = created.Sku;
                Price = created.Price;
                Packaging = created.Packaging;
                // Copied into a list of this model's own rather than held by reference, so the
                // event stays the immutable record of what happened whatever the model does next.
                Keywords = [..created.Keywords];
                Variants = [..created.Variants];
                return true;

            case ProductDetailsChangedEvent changed:
                Name = changed.Name;
                Price = changed.Price;
                return true;

            case ProductDiscontinuedEvent:
                Discontinued = true;
                return true;

            case ProductDeletedEvent:
                Exists = false;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies the product itself: one tag, holding everything ever recorded about it.
/// </summary>
/// <remarks>
/// Enough for any decision that depends only on this product — deleting it, renaming it, repricing
/// it. Creating one needs more, which is what <see cref="ProductCreationId"/> is for.
/// </remarks>
public class ProductId(string productId) : IDcbAggregateId<Product>
{
    public string Id { get; } = productId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("product", productId));
}

/// <summary>
/// Identifies the decision to create a product, and carries the wider boundary that decision is
/// folded from.
/// </summary>
/// <remarks>
/// The SKU is in the boundary because creating is the decision that has to see whether another
/// product already claimed it. A product's SKU is fixed at creation here, so the boundary stays
/// stable for a given <see cref="Id"/> — which is what snapshots rely on.
/// </remarks>
public class ProductCreationId(string productId, string sku) : IDcbAggregateId<Product>
{
    public string Id { get; } = productId;

    public TagQuery Boundary { get; } =
        TagQuery.AnyOf(new Tag("product", productId), new Tag("sku", sku));
}
