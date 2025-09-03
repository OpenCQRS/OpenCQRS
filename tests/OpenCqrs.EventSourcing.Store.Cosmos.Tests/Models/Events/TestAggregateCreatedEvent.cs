using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;

[DomainEventType("TestAggregateCreated")]
public record TestAggregateCreatedEvent(string Id, string Name, string Description) : IDomainEvent;
