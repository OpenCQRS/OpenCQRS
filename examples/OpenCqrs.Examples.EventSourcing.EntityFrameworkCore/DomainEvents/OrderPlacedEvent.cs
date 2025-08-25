using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.DomainEvents;

[DomainEventType("OrderPlaced")]
public record OrderPlacedEvent(Guid OrderId, decimal Amount) : IDomainEvent;
