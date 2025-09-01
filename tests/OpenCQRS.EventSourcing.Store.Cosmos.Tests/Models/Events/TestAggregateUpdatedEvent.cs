using OpenCqrs.EventSourcing.Domain;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;

[DomainEventType("TestAggregateUpdated")]
public record TestAggregateUpdatedEvent(string Id, string Name, string Description) : IDomainEvent;
