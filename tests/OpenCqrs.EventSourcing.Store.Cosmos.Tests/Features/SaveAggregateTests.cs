using System.Diagnostics;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Streams;
using OpenCqrs.Results;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests.Features;

public class SaveAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAnotherEventWithTheExpectedSequenceIsAlreadyStored_ThenReturnsConcurrencyExceptionFailure()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var saveResult = await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        using (new AssertionScope())
        {
            saveResult.IsSuccess.Should().BeFalse();
            saveResult.Failure.Should().NotBeNull();
            saveResult.Failure.ErrorCode.Should().Be(ErrorCode.Error);
            saveResult.Failure.Title.Should().Be("Error");
            saveResult.Failure.Description.Should().Be("There was an error when processing the request");

            var activityEvent = Activity.Current?.Events.SingleOrDefault(e => e.Name == "Concurrency exception");
            activityEvent.Should().NotBeNull();
            activityEvent.Value.Tags.First().Key.Should().Be("streamId");
            activityEvent.Value.Tags.First().Value.Should().Be(streamId.Id);
            activityEvent.Value.Tags.Skip(1).First().Key.Should().Be("expectedEventSequence");
            activityEvent.Value.Tags.Skip(1).First().Value.Should().Be(0);
            activityEvent.Value.Tags.Skip(2).First().Key.Should().Be("latestEventSequence");
            activityEvent.Value.Tags.Skip(2).First().Value.Should().Be(1);
        }
    }

    [Fact]
    public async Task GivenNewAggregateSaved_ThenAggregateAndEventDocumentsAreStored()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        var saveResult = await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);
        var eventDocuments = await DataStore.GetEventDocuments(streamId);

        using (new AssertionScope())
        {
            saveResult.IsSuccess.Should().BeTrue();

            aggregateDocument.Value.Should().NotBeNull();
            aggregateDocument.Value.AggregateType.Should().Be("TestAggregate1:1");
            aggregateDocument.Value.Version.Should().Be(1);
            aggregateDocument.Value.LatestEventSequence.Should().Be(1);

            eventDocuments.Value.Should().NotBeNull();
            eventDocuments.Value[0].EventType.Should().Be("TestAggregateCreated:1");
            eventDocuments.Value[0].Sequence.Should().Be(1);
        }
    }

    [Fact]
    public async Task GivenAggregateIsUpdated_ThenAggregateDocumentVersionIncreasesAndAllEventDocumentsAreStored()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var updatedAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);
        aggregate = updatedAggregateResult.Value!;
        aggregate.Update("Updated Name", "Updated Description");

        var saveResult = await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 1);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);
        var eventDocuments = await DataStore.GetEventDocuments(streamId);

        using (new AssertionScope())
        {
            saveResult.IsSuccess.Should().BeTrue();

            aggregateDocument.Value.Should().NotBeNull();
            aggregateDocument.Value.AggregateType.Should().Be("TestAggregate1:1");
            aggregateDocument.Value.Version.Should().Be(2);
            aggregateDocument.Value.LatestEventSequence.Should().Be(2);

            eventDocuments.Value!.Count.Should().Be(2);
            eventDocuments.Value[0].EventType.Should().Be("TestAggregateCreated:1");
            eventDocuments.Value[0].Sequence.Should().Be(1);
            eventDocuments.Value[1].EventType.Should().Be("TestAggregateUpdated:1");
            eventDocuments.Value[1].Sequence.Should().Be(2);
        }
    }

    [Fact]
    public async Task GivenNewAggregateSaved_WhenMultipleEventsAdded_ThenAggregateAndAllEventDocumentsAreStored()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name", "Updated Description");

        var saveResult = await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);
        var eventDocuments = await DataStore.GetEventDocuments(streamId);

        using (new AssertionScope())
        {
            saveResult.IsSuccess.Should().BeTrue();

            aggregateDocument.Value.Should().NotBeNull();
            aggregateDocument.Value.AggregateType.Should().Be("TestAggregate1:1");
            aggregateDocument.Value.Version.Should().Be(2);
            aggregateDocument.Value.LatestEventSequence.Should().Be(2);

            eventDocuments.Value!.Count.Should().Be(2);
            eventDocuments.Value[0].EventType.Should().Be("TestAggregateCreated:1");
            eventDocuments.Value[0].Sequence.Should().Be(1);
            eventDocuments.Value[1].EventType.Should().Be("TestAggregateUpdated:1");
            eventDocuments.Value[1].Sequence.Should().Be(2);
        }
    }

    [Fact]
    public async Task GivenEventsNotHandledByTheAggregateStored_WhenAggregateIsUpdated_ThenLastEventSequenceIsGreaterThenAggregateVersion()
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

        var aggregateResult = await DomainService.GetAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            aggregateResult.IsSuccess.Should().BeTrue();

            aggregateResult.Value.Should().NotBeNull();

            aggregateResult.Value.StreamId.Should().Be(streamId.Id);
            aggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            aggregateResult.Value.Version.Should().Be(2);
            aggregateResult.Value.LatestEventSequence.Should().Be(6);

            aggregateResult.Value.Id.Should().Be(id);
            aggregateResult.Value.Name.Should().Be("Updated Name");
            aggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }

    [Fact]
    public async Task GivenNewAggregateSaved_ThenAuditablePropertiesArePopulated()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        var now = new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        TimeProvider.SetUtcNow(now);

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);
        var eventDocuments = await DataStore.GetEventDocuments(streamId);

        using (new AssertionScope())
        {
            aggregateDocument.Value.Should().NotBeNull();
            aggregateDocument.Value.CreatedBy.Should().Be("TestUser");
            aggregateDocument.Value.CreatedDate.Should().Be(now);
            aggregateDocument.Value.UpdatedBy.Should().Be("TestUser");
            aggregateDocument.Value.UpdatedDate.Should().Be(now);

            eventDocuments.Value.Should().NotBeNull();
            eventDocuments.Value[0].Should().NotBeNull();
            eventDocuments.Value[0].CreatedBy.Should().Be("TestUser");
            eventDocuments.Value[0].CreatedDate.Should().Be(now);
        }
    }

    [Fact]
    public async Task GivenAggregateUpdated_ThenAuditablePropertiesArePopulated()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        var createDate = new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc);
        TimeProvider.SetUtcNow(createDate);
        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        var updateDate = new DateTime(2024, 6, 15, 18, 0, 0, DateTimeKind.Utc);
        TimeProvider.SetUtcNow(updateDate);
        var aggregateToUpdateResult = await DomainService.GetAggregate(streamId, aggregateId);
        aggregateToUpdateResult.Value!.Update("Updated Name", "Updated Description");
        await DomainService.SaveAggregate(streamId, aggregateId, aggregateToUpdateResult.Value, expectedEventSequence: 1);

        var aggregateDocument = await DataStore.GetAggregateDocument(streamId, aggregateId);
        var eventDocuments = await DataStore.GetEventDocuments(streamId);

        using (new AssertionScope())
        {
            aggregateDocument.Value.Should().NotBeNull();
            aggregateDocument.Value.CreatedBy.Should().Be("TestUser");
            aggregateDocument.Value.CreatedDate.Should().Be(createDate);
            aggregateDocument.Value.UpdatedBy.Should().Be("TestUser");
            aggregateDocument.Value.UpdatedDate.Should().Be(updateDate);

            eventDocuments.Value.Should().NotBeNull();
            eventDocuments.Value[0].CreatedBy.Should().Be("TestUser");
            eventDocuments.Value[0].CreatedDate.Should().Be(createDate);
        }
    }
}
