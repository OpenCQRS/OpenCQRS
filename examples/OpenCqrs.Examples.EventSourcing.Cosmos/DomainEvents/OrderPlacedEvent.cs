using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.DomainEvents;

[DomainEventType("OrderPlaced")]
public record OrderPlacedEvent(Guid OrderId, decimal Amount) : IDomainEvent;
