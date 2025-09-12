using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Streams;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Features;

public class UpdateAggregateTests : TestBase
{
    [Fact]
    public async Task GivenEventsHandledByTheAggregateAreStoredSeparately_WhenAggregateIsUpdated_ThenAggregateVersionIsIncreasedAndTheUpdatedAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        await DomainService.SaveEvents(streamId, [new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")], expectedEventSequence: 1);
        var updatedAggregateResult = await DomainService.UpdateAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            updatedAggregateResult.IsSuccess.Should().BeTrue();

            updatedAggregateResult.Value.Should().NotBeNull();

            updatedAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            updatedAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            updatedAggregateResult.Value.Version.Should().Be(2);

            updatedAggregateResult.Value.Id.Should().Be(id);
            updatedAggregateResult.Value.Name.Should().Be("Updated Name");
            updatedAggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }
    
    // TODO: store snapshot disabled by options for new aggregate - upgrade aggregate - stores new aggregate
    // TODO: store snapshot disabled by options for existing aggregate - upgrade aggregate - stores upgraded aggregate
    
    // TODO: store snapshot disabled by override for new aggregate - upgrade aggregate - stores new aggregate           
    // TODO: store snapshot disabled by override for existing aggregate - upgrade aggregate - stores upgraded aggregate 
}

