using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.EventSourcing.Domain;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Streams;
using Xunit;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Features;

public class GetDomainEventsAppliedToAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAggregateSaved_ThenOnlyAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate2Id(id);
        var aggregate = new TestAggregate2(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var domainEvents = new IDomainEvent[]
        {
            new SomethingHappenedEvent("Something1"),
            new SomethingHappenedEvent("Something2"),
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        };
        await DomainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 1);

        var aggregateToUpdateResult = await DomainService.GetAggregate(streamId, aggregateId);
        aggregateToUpdateResult.Value!.Update("Updated Name", "Updated Description");
        await DomainService.SaveAggregate(streamId, aggregateId, aggregateToUpdateResult.Value, expectedEventSequence: 5);

        var result = await DomainService.GetDomainEventsAppliedToAggregate(aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(2);
        }
    }

    // [Fact]
    // public async Task GivenStoredEventForAnAggregateIsAppliedInAnotherAggregate_ThenOnlyAggregateEventsAppliedAreReturned()
    // {
    //     var id = Guid.NewGuid().ToString();
    //     var streamId = new TestStreamId(id);
    //
    //     var testAggregate1Key = new TestAggregate1Id(id);
    //     var testAggregate2Key = new TestAggregate2Id(id);
    //     var testAggregate1 = new TestAggregate1(id, "Test Name", "Test Description");
    //
    //     await using var dbContext = Shared.CreateTestDbContext();
    //
    //     var trackResult = await dbContext.TrackAggregate(streamId, testAggregate1Key, testAggregate1, expectedEventSequence: 0);
    //     await dbContext.TrackEventEntities(streamId, testAggregate2Key, trackResult.Value.EventEntities!, expectedEventSequence: 0);
    //     await dbContext.Save();
    //
    //     var result = await dbContext.GetDomainEventsAppliedToAggregate(testAggregate2Key);
    //
    //     using (new AssertionScope())
    //     {
    //         result.IsSuccess.Should().BeTrue();
    //         result.Value.Should().NotBeNull();
    //         result.Value.Count.Should().Be(1);
    //     }
    // }
    //
    // [Fact]
    // public async Task GivenAggregateDoesNotExist_WhenEventsAreStoredAndAppliedWhenGettingTheAggregate_ThenAggregateEventsAppliedAreReturned()
    // {
    //     var id = Guid.NewGuid().ToString();
    //     var streamId = new TestStreamId(id);
    //     var aggregateId = new TestAggregate1Id(id);
    //
    //     await using var dbContext = Shared.CreateTestDbContext();
    //
    //     dbContext.Add(new TestAggregateCreatedEvent(id, "Test Name", "Test Description").ToEventEntity(streamId, sequence: 1));
    //     dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
    //     await dbContext.SaveChangesAsync();
    //
    //     await dbContext.GetAggregate(streamId, aggregateId);
    //     var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);
    //
    //     using (new AssertionScope())
    //     {
    //         result.IsSuccess.Should().BeTrue();
    //         result.Value.Should().NotBeNull();
    //         result.Value.Count.Should().Be(2);
    //     }
    // }
    //
    // [Fact]
    // public async Task GivenDomainEventsHandledByTheAggregateAreStoredSeparately_WhenApplyNewEventsIsRequestedWhenGettingTheAggregate_ThenAggregateEventsAppliedAreReturned()
    // {
    //     var id = Guid.NewGuid().ToString();
    //     var streamId = new TestStreamId(id);
    //     var aggregateId = new TestAggregate1Id(id);
    //     var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
    //
    //     await using var dbContext = Shared.CreateTestDbContext();
    //     await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
    //
    //     dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
    //     await dbContext.Save();
    //
    //     await dbContext.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
    //     var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);
    //
    //     using (new AssertionScope())
    //     {
    //         result.IsSuccess.Should().BeTrue();
    //         result.Value.Should().NotBeNull();
    //         result.Value.Count.Should().Be(2);
    //     }
    // }
    //
    // [Fact]
    // public async Task GivenDomainEventsHandledByTheAggregateAreStoredSeparately_WhenAggregateIsUpdated_ThenAggregateEventsAppliedAreReturned()
    // {
    //     var id = Guid.NewGuid().ToString();
    //     var streamId = new TestStreamId(id);
    //     var aggregateId = new TestAggregate1Id(id);
    //     var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
    //
    //     await using var dbContext = Shared.CreateTestDbContext();
    //     await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
    //
    //     dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
    //     await dbContext.Save();
    //
    //     await dbContext.UpdateAggregate(streamId, aggregateId);
    //     var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);
    //
    //     using (new AssertionScope())
    //     {
    //         result.IsSuccess.Should().BeTrue();
    //         result.Value.Should().NotBeNull();
    //         result.Value.Count.Should().Be(2);
    //     }
    // }
}
