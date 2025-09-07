using FluentAssertions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Streams;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Features;

public class GetDomainEventsTests : TestBase
{
    [Fact]
    public async Task GiveMultipleDomainEventsSaved_WhenAllDomainEventsAreRequested_ThenAllDomainEventsAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name", "Updated Description");
        aggregate.Update("Updated Name 2", "Updated Description 2");
        aggregate.Update("Updated Name 3", "Updated Description 3");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var domainEvents = await DomainService.GetDomainEvents(streamId);

        domainEvents.Value!.Count.Should().Be(4);
    }

    [Fact]
    public async Task GiveMultipleDomainEventsSaved_WhenOnlyDomainEventsUpToASpecificSequenceAreRequested_ThenOnlyDomainEventsUpToTheSpecificSequenceAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name", "Updated Description");
        aggregate.Update("Updated Name 2", "Updated Description 2");
        aggregate.Update("Updated Name 3", "Updated Description 3");

        await using var dbContext = Shared.CreateTestDbContext();
        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var domainEvents = await dbContext.GetDomainEventsUpToSequence(streamId, upToSequence: 3);

        domainEvents.Count.Should().Be(3);
    }

    [Fact]
    public async Task GiveMultipleDomainEventsSaved_WhenOnlyDomainEventsFromASpecificSequenceAreRequested_ThenOnlyDomainEventsFromTheSpecificSequenceAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name", "Updated Description");
        aggregate.Update("Updated Name 2", "Updated Description 2");
        aggregate.Update("Updated Name 3", "Updated Description 3");

        await using var dbContext = Shared.CreateTestDbContext();
        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var domainEvents = await dbContext.GetDomainEventsFromSequence(streamId, fromSequence: 3);

        domainEvents.Count.Should().Be(2);
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsBetweenSpecificSequencesAreRequested_ThenOnlyDomainEventsBetweenSpecificSequencesAreReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name 1", "Updated Description 1");
        aggregate.Update("Updated Name 2", "Updated Description 2");
        aggregate.Update("Updated Name 3", "Updated Description 3");
        aggregate.Update("Updated Name 4", "Updated Description 4");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var domainEvents = await DomainService.GetDomainEventsBetweenSequences(streamId, fromSequence: 2, toSequence: 4);

        domainEvents.Value!.Count.Should().Be(3);
    }
}
