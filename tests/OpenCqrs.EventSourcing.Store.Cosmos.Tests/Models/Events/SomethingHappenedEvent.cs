using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;

[EventType("SomethingHappened")]
public record SomethingHappenedEvent(string Something) : IEvent;
