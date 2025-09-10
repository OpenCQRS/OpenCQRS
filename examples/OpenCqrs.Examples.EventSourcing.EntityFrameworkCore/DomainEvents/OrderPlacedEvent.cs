using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.events;

[EventType("OrderPlaced")]
public record OrderPlacedEvent(Guid OrderId, decimal Amount) : IEvent;
