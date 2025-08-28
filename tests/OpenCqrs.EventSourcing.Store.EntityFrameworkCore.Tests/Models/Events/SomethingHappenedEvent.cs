using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

[DomainEventType("SomethingHappened")]
public record SomethingHappenedEvent(string Something) : IDomainEvent;
