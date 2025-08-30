using OpenCqrs.EventSourcing.Domain;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;

[DomainEventType("SomethingHappened")]
public record SomethingHappenedEvent(string Something) : IDomainEvent;
