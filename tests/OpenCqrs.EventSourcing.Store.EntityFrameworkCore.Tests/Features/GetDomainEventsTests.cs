using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Time.Testing;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;
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

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsUpToASpecificDateAreRequested_ThenDomainEventsUpToASpecificDateAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new SomethingHappenedEvent("Something2")
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something5"),
            new SomethingHappenedEvent("Something6")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsUpToDate(streamId, upToDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)));
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(4);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something1");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something2");
            result.Value[2].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something3");
            result.Value[3].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something4");
        }
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsUpToASpecificDateFilteredByEventTypeAreRequested_ThenDomainEventsUpToASpecificDateFilteredByEventTypeAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new TestAggregateCreatedEvent(Guid.NewGuid().ToString(), "Test Name", "Test Description"),
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something2"),
            new TestAggregateUpdatedEvent(Guid.NewGuid().ToString(), "Updated Name", "Updated Description")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsUpToDate(streamId, upToDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)), eventTypeFilter: [typeof(SomethingHappenedEvent)]);
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(2);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something1");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something2");
        }
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsFromASpecificDateAreRequested_ThenDomainEventsFromASpecificDateAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new SomethingHappenedEvent("Something2")
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something5"),
            new SomethingHappenedEvent("Something6")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsFromDate(streamId, fromDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)));
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(4);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something3");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something4");
            result.Value[2].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something5");
            result.Value[3].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something6");
        }
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsFromASpecificDateFilteredByEventTypeAreRequested_ThenDomainEventsFromASpecificDateFilteredByEventTypeAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new TestAggregateCreatedEvent(Guid.NewGuid().ToString(), "Test Name", "Test Description"),
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something2"),
            new TestAggregateUpdatedEvent(Guid.NewGuid().ToString(), "Updated Name", "Updated Description")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsFromDate(streamId, fromDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)), eventTypeFilter: [typeof(SomethingHappenedEvent)]);
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(3);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something2");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something3");
            result.Value[2].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something4");
        }
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsBetweenSpecificDatesAreRequested_ThenDomainEventsBetweenSpecificDatesAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new SomethingHappenedEvent("Something2")
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something5"),
            new SomethingHappenedEvent("Something6")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsBetweenDates(streamId,
            fromDate: new DateTimeOffset(new DateTime(2024, 6, 10, 12, 10, 25)),
            toDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)));
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(4);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something1");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something2");
            result.Value[2].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something3");
            result.Value[3].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something4");
        }
    }

    [Fact]
    public async Task GiveMultipleDomainEventsStored_WhenOnlyDomainEventsBetweenSpecificDatesFilteredByEventTypeAreRequested_ThenDomainEventsBetweenSpecificDatesFilteredByEventTypeAreReturned()
    {
        var streamId = new TestStreamId(Guid.NewGuid().ToString());
        var timeProvider = new FakeTimeProvider();

        using var domainService = Shared.CreateDomainService(timeProvider, Shared.CreateHttpContextAccessor());

        timeProvider.SetUtcNow(new DateTime(2024, 6, 10, 12, 10, 25));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something1"),
            new TestAggregateCreatedEvent(Guid.NewGuid().ToString(), "Test Name", "Test Description"),
        ], expectedEventSequence: 0);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 48));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something2"),
            new TestAggregateUpdatedEvent(Guid.NewGuid().ToString(), "Updated Name", "Updated Description")
        ], expectedEventSequence: 2);

        timeProvider.SetUtcNow(new DateTime(2024, 6, 15, 17, 45, 49));
        await domainService.SaveDomainEvents(streamId, [
            new SomethingHappenedEvent("Something3"),
            new SomethingHappenedEvent("Something4")
        ], expectedEventSequence: 4);

        var result = await domainService.GetDomainEventsBetweenDates(streamId,
            fromDate: new DateTimeOffset(new DateTime(2024, 6, 10, 12, 10, 25)),
            toDate: new DateTimeOffset(new DateTime(2024, 6, 15, 17, 45, 48)),
            eventTypeFilter: [typeof(SomethingHappenedEvent)]);
        using (new AssertionScope())
        {
            result.Value!.Count.Should().Be(2);
            result.Value[0].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something1");
            result.Value[1].Should().BeOfType<SomethingHappenedEvent>().Which.Something.Should().Be("Something2");
        }
    }
}
