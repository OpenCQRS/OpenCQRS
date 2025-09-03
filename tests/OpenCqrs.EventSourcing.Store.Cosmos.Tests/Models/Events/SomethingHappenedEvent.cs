using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;

[DomainEventType("SomethingHappened")]
public record SomethingHappenedEvent(string Something) : IDomainEvent;
