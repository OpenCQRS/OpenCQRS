using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Streams;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Features;

public class GetDomainEventsAppliedToAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAggregateSaved_ThenOnlyAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate2Id(id);
        var aggregate = new TestAggregate2(id, "Test Name", "Test Description");

        await using var dbContext = Shared.CreateTestDbContext();

        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        dbContext.Add(new SomethingHappenedEvent("Something1").ToEventEntity(streamId, sequence: 2));
        dbContext.Add(new SomethingHappenedEvent("Something2").ToEventEntity(streamId, sequence: 3));
        dbContext.Add(new SomethingHappenedEvent("Something3").ToEventEntity(streamId, sequence: 4));
        dbContext.Add(new SomethingHappenedEvent("Something4").ToEventEntity(streamId, sequence: 5));
        await dbContext.SaveChangesAsync();

        var aggregateToUpdateResult = await dbContext.GetAggregate(streamId, aggregateId);
        aggregateToUpdateResult.Value!.Update("Updated Name", "Updated Description");
        await dbContext.SaveAggregate(streamId, aggregateId, aggregateToUpdateResult.Value, expectedEventSequence: 5);

        var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(2);
        }
    }

    [Fact]
    public async Task GivenStoredEventForAnAggregateIsAppliedInAnotherAggregate_ThenOnlyAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);

        var testAggregate1Key = new TestAggregate1Id(id);
        var testAggregate2Key = new TestAggregate2Id(id);
        var testAggregate1 = new TestAggregate1(id, "Test Name", "Test Description");

        await using var dbContext = Shared.CreateTestDbContext();

        var trackResult = await dbContext.TrackWithAggregate(streamId, testAggregate1Key, testAggregate1, expectedEventSequence: 0);
        await dbContext.TrackWithEventEntities(streamId, testAggregate2Key, trackResult.Value.EventEntities!, expectedEventSequence: 0);
        await dbContext.Save();

        var result = await dbContext.GetDomainEventsAppliedToAggregate(testAggregate2Key);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(1);
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenEventsAreStoredAndAppliedWhenGettingTheAggregate_ThenAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        await using var dbContext = Shared.CreateTestDbContext();

        dbContext.Add(new TestAggregateCreatedEvent(id, "Test Name", "Test Description").ToEventEntity(streamId, sequence: 1));
        dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
        await dbContext.SaveChangesAsync();

        await dbContext.GetAggregate(streamId, aggregateId);
        var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(2);
        }
    }

    [Fact]
    public async Task GivenDomainEventsHandledByTheAggregateAreStoredSeparately_WhenApplyNewEventsIsRequestedWhenGettingTheAggregate_ThenAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await using var dbContext = Shared.CreateTestDbContext();
        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
        await dbContext.Save();

        await dbContext.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
        var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(2);
        }
    }

    [Fact]
    public async Task GivenDomainEventsHandledByTheAggregateAreStoredSeparately_WhenAggregateIsUpdated_ThenAggregateEventsAppliedAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await using var dbContext = Shared.CreateTestDbContext();
        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        dbContext.Add(new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
        await dbContext.Save();

        await dbContext.UpdateAggregate(streamId, aggregateId);
        var result = await dbContext.GetDomainEventsAppliedToAggregate(aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Count.Should().Be(2);
        }
    }
}
