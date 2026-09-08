using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Dcb.Events;

/// <summary>
/// A product was added to the catalogue.
/// </summary>
/// <remarks>
/// Appended under two tags, <c>product:{id}</c> and <c>sku:{sku}</c>, which is what lets a later
/// creation see that the code is taken without anything having to keep an index of codes.
/// </remarks>
[EventType("ProductCreated")]
public record ProductCreatedEvent(string ProductId, string Name, string Sku, decimal Price) : IEvent;

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
