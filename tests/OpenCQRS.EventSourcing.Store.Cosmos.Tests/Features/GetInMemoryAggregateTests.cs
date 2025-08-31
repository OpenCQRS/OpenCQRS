using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.EventSourcing.Domain;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Streams;
using Xunit;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Features;

public class GetInMemoryAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAggregateDoesExist_ThenTheInMemoryAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        var getAggregateResult = await DomainService.GetInMemoryAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            getAggregateResult.Value.Version.Should().Be(1);

            getAggregateResult.Value.Id.Should().Be(aggregate.Id);
            getAggregateResult.Value.Name.Should().Be(aggregate.Name);
            getAggregateResult.Value.Description.Should().Be(aggregate.Description);
        }
    }

    [Fact]
    public async Task GivenAggregateDoesExist_ThenTheInMemoryAggregateUpToSequenceIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        var domainEvents = new IDomainEvent[]
        {
            new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")
        };
        await DomainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 1);
        
        var getAggregateResult = await DomainService.GetInMemoryAggregate(streamId, aggregateId, upToSequence: 1);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            getAggregateResult.Value.Version.Should().Be(1);

            getAggregateResult.Value.Id.Should().Be(aggregate.Id);
            getAggregateResult.Value.Name.Should().Be(aggregate.Name);
            getAggregateResult.Value.Description.Should().Be(aggregate.Description);
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenEventsHandledByTheAggregateTypeAreStored_ThenTheInMemoryAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var domainEvents = new IDomainEvent[]
        {
            new TestAggregateCreatedEvent(id, "Test Name", "Test Description"),
            new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")
        };
        await DomainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 0);

        var aggregateResult = await DomainService.GetInMemoryAggregate(streamId, aggregateId);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);

        using (new AssertionScope())
        {
            aggregateResult.IsSuccess.Should().BeTrue();

            aggregateResult.Value.Should().NotBeNull();
            aggregateResult.Value.StreamId.Should().Be(streamId.Id);
            aggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            aggregateResult.Value.Version.Should().Be(2);
            aggregateResult.Value.Id.Should().Be(id);
            aggregateResult.Value.Name.Should().Be("Updated Name");
            aggregateResult.Value.Description.Should().Be("Updated Description");

            aggregateDocument.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenEventsHandledByTheAggregateTypeAreStored_ThenTheInMemoryAggregateUpToSequenceIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var domainEvents = new IDomainEvent[]
        {
            new TestAggregateCreatedEvent(id, "Test Name", "Test Description"),
            new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")
        };
        await DomainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 0);

        var aggregateResult = await DomainService.GetInMemoryAggregate(streamId, aggregateId, upToSequence: 1);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);

        using (new AssertionScope())
        {
            aggregateResult.IsSuccess.Should().BeTrue();

            aggregateResult.Value.Should().NotBeNull();
            aggregateResult.Value.StreamId.Should().Be(streamId.Id);
            aggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            aggregateResult.Value.Version.Should().Be(1);
            aggregateResult.Value.Id.Should().Be(id);
            aggregateResult.Value.Name.Should().Be("Test Name");
            aggregateResult.Value.Description.Should().Be("Test Description");

            aggregateDocument.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenNoEventsAreStored_TheDefaultAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var result = await DomainService.GetInMemoryAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Failure.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Version.Should().Be(0);
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenNoEventsHandledByTheAggregateTypeAreStored_TheDefaultAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var domainEvents = new IDomainEvent[]
        {
            new SomethingHappenedEvent(Something: "Something")
        };
        await DomainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 1);

        var result = await DomainService.GetInMemoryAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Failure.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Version.Should().Be(0);
        }
    }
}
