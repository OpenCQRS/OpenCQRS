using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

[EventType("SomethingHappened")]
public record SomethingHappenedEvent(string Something) : IEvent;
