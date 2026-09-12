using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Dcb.Events;

/// <summary>
/// A product was added to the catalogue.
/// </summary>
/// <remarks>
/// <para>
/// Appended under two tags, <c>product:{id}</c> and <c>sku:{sku}</c>, which is what lets a later
/// creation see that the code is taken without anything having to keep an index of codes.
/// </para>
/// <para>
/// Not everything an event carries is a number or a string. <see cref="Packaging"/> is a value of
/// its own, <see cref="Keywords"/> is a list of plain ones, and <see cref="Variants"/> is a list of
/// values — all three go through the serializer whole, into the log and into the snapshot of any
/// model that folds them. Nothing about the fold changes; only what this one event is carrying.
/// </para>
/// </remarks>
[EventType("ProductCreated")]
public record ProductCreatedEvent(
    string ProductId,
    string Name,
    string Sku,
    decimal Price,
    PackagedSize Packaging,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<ProductVariant> Variants) : IEvent;

/// <summary>
/// What a product measures and weighs once it is boxed, in centimetres and kilogrammes.
/// </summary>
/// <remarks>
/// A value rather than an entity: nothing refers to one, nothing edits one, and two of the same
/// measurements are the same size. A record says that, and gives the serializer a constructor to
/// read the stored event back into.
/// </remarks>
public record PackagedSize(decimal WidthCm, decimal HeightCm, decimal DepthCm, decimal WeightKg);

/// <summary>
/// One buyable variation of a product — a finish or a colourway — and what it does to the price.
/// </summary>
/// <remarks>
/// <see cref="PriceDifference"/> is a difference rather than a price, so a repricing of the product
/// moves every variant with it and no variant can quietly go stale.
/// </remarks>
public record ProductVariant(string Code, string Name, decimal PriceDifference);

/// <summary>
/// A product's name or price changed.
/// </summary>
/// <remarks>
/// Not tagged with the SKU: nothing about this event affects whether a code is free, so the
/// decision that creates a product has no reason to read it.
/// </remarks>
[EventType("ProductDetailsChanged")]
public record ProductDetailsChangedEvent(string ProductId, string Name, decimal Price) : IEvent;

/// <summary>
/// A product was removed from the catalogue.
/// </summary>
/// <remarks>
/// Carries the SKU as well as the id because it is appended under both tags. Without the SKU tag a
/// later creation reusing the code would fold the old <see cref="ProductCreatedEvent"/>, find the
/// code taken, and refuse — the deletion would be invisible to the very decision it should free.
/// </remarks>
[EventType("ProductDeleted")]
public record ProductDeletedEvent(string ProductId, string Sku) : IEvent;

/// <summary>
/// A product was taken off sale without being removed from the catalogue.
/// </summary>
/// <remarks>
/// A separate fact from deletion, and the reason both exist: a discontinued product still has a
/// history, still holds its SKU, and can come back. Tagged with the product alone.
/// </remarks>
[EventType("ProductDiscontinued")]
public record ProductDiscontinuedEvent(string ProductId, string Reason) : IEvent;
