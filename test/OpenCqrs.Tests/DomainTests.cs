using FluentAssertions;
using OpenCqrs.Domain;
using OpenCqrs.Tests.Models;
using Xunit;

namespace OpenCqrs.Tests;

public class DomainTests
{
    public DomainTests()
    {
        TypeBindings.DomainEventBindings = new Dictionary<string, Type>
        {
            {"TestDomainEvent|v:1", typeof(TestDomainEventV1)},
            {"TestDomainEvent|v:2", typeof(TestDomainEventV2)}
        };
        
        TypeBindings.AggregateBindings = new Dictionary<string, Type>
        {
            {"TestAggregate|v:1", typeof(TestAggregate)}
        };
    }

    [Fact]
    public void ShouldAddDomainEventToUncommittedEvents()
    {
        var testAggregate = new TestAggregate("test-id", "test-name", "test-description");

        testAggregate.UncommittedEvents.Count().Should().Be(1);
    }
}
