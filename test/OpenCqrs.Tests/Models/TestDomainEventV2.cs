using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Models;

[DomainEventType("TestDomainEvent", version: 2)]
public record TestDomainEventV2(string Id, string Name, string Summary) : IDomainEvent;
