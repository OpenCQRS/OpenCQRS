using OpenCqrs.EventSourcing.Domain;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;

[DomainEventType("TestAggregateCreated")]
public record TestAggregateCreatedEvent(string Id, string Name, string Description) : IDomainEvent;
