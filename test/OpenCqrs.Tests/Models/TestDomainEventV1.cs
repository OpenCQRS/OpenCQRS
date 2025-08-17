using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Models;

[DomainEventType("TestDomainEvent")]
public record TestDomainEventV1(string Id, string Name, string Description) : IDomainEvent;
