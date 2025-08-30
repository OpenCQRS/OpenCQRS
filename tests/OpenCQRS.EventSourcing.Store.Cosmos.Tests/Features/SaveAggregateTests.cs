// using FluentAssertions;
// using FluentAssertions.Execution;
// using Microsoft.Extensions.Time.Testing;
// using OpenCqrs.EventSourcing.Domain;
// using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
// using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;
// using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Streams;
// using OpenCqrs.Results;
// using Xunit;
//
// namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Features;
//
// public class SaveAggregateTests : TestBase
// {
//     [Fact]
//     public async Task GivenAnotherEventWithTheExpectedSequenceIsAlreadyStored_ThenReturnsConcurrencyExceptionFailure()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//         
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//         var saveResult = await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//
//         using (new AssertionScope())
//         {
//             saveResult.IsSuccess.Should().BeFalse();
//             saveResult.Failure.Should().NotBeNull();
//             saveResult.Failure.ErrorCode.Should().Be(ErrorCode.Error);
//             saveResult.Failure.Title.Should().Be("Concurrency exception");
//             saveResult.Failure.Description.Should().Be("Expected event sequence 0 but found 1");
//         }
//     }
//
//     [Fact]
//     public async Task GivenNewAggregateSaved_ThenAggregateAndEventEntitiesAreStored()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//
//         var saveResult = await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//         var aggregateEntity = await CosmosDataStore.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));
//         var eventEntity = await CosmosDataStore.Events.AsNoTracking().FirstOrDefaultAsync(a => a.StreamId == streamId.Id);
//
//         using (new AssertionScope())
//         {
//             saveResult.IsSuccess.Should().BeTrue();
//
//             aggregateEntity.Should().NotBeNull();
//             aggregateEntity.TypeName.Should().Be("TestAggregate1");
//             aggregateEntity.Version.Should().Be(1);
//             aggregateEntity.LatestEventSequence.Should().Be(1);
//
//             eventEntity.Should().NotBeNull();
//             eventEntity.TypeName.Should().Be("TestAggregateCreated");
//             eventEntity.Sequence.Should().Be(1);
//         }
//     }
//
//     [Fact]
//     public async Task GivenAggregateIsUpdated_ThenAggregateEntityVersionIncreasesAndAllEventEntitiesAreStored()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//         
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//         var updatedAggregateResult = await CosmosDataStore.GetAggregate(streamId, aggregateId);
//         aggregate = updatedAggregateResult.Value!;
//         aggregate.Update("Updated Name", "Updated Description");
//
//         var saveResult = await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 1);
//         var aggregateEntity = await CosmosDataStore.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));
//         var eventEntities = await CosmosDataStore.Events.AsNoTracking().Where(a => a.StreamId == streamId.Id).ToListAsync();
//
//         using (new AssertionScope())
//         {
//             saveResult.IsSuccess.Should().BeTrue();
//
//             aggregateEntity.Should().NotBeNull();
//             aggregateEntity.TypeName.Should().Be("TestAggregate1");
//             aggregateEntity.Version.Should().Be(2);
//             aggregateEntity.LatestEventSequence.Should().Be(2);
//
//             eventEntities.Count.Should().Be(2);
//             eventEntities[0].TypeName.Should().Be("TestAggregateCreated");
//             eventEntities[0].Sequence.Should().Be(1);
//             eventEntities[1].TypeName.Should().Be("TestAggregateUpdated");
//             eventEntities[1].Sequence.Should().Be(2);
//         }
//     }
//
//     [Fact]
//     public async Task GivenNewAggregateSaved_WhenMultipleEventsAdded_ThenAggregateAndAllEventEntitiesAreStored()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//         aggregate.Update("Updated Name", "Updated Description");
//         
//         var saveResult = await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//         var aggregateEntity = await CosmosDataStore.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));
//         var eventEntities = await CosmosDataStore.Events.AsNoTracking().Where(a => a.StreamId == streamId.Id).ToListAsync();
//
//         using (new AssertionScope())
//         {
//             saveResult.IsSuccess.Should().BeTrue();
//
//             aggregateEntity.Should().NotBeNull();
//             aggregateEntity.TypeName.Should().Be("TestAggregate1");
//             aggregateEntity.Version.Should().Be(2);
//             aggregateEntity.LatestEventSequence.Should().Be(2);
//
//             eventEntities.Count.Should().Be(2);
//             eventEntities[0].TypeName.Should().Be("TestAggregateCreated");
//             eventEntities[0].Sequence.Should().Be(1);
//             eventEntities[1].TypeName.Should().Be("TestAggregateUpdated");
//             eventEntities[1].Sequence.Should().Be(2);
//         }
//     }
//
//     [Fact]
//     public async Task GivenEventsNotHandledByTheAggregateStored_WhenAggregateIsUpdated_ThenLastEventSequenceIsGreaterThenAggregateVersion()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate2Id(id);
//         var aggregate = new TestAggregate2(id, "Test Name", "Test Description");
//         
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//
//         var domainEvents = new IDomainEvent[]
//         {
//             new SomethingHappenedEvent("Something1"),
//             new SomethingHappenedEvent("Something2"),
//             new SomethingHappenedEvent("Something3"),
//             new SomethingHappenedEvent("Something4")
//         };
//         await CosmosDataStore.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: 1);
//
//         var aggregateToUpdateResult = await CosmosDataStore.GetAggregate(streamId, aggregateId);
//         aggregateToUpdateResult.Value!.Update("Updated Name", "Updated Description");
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregateToUpdateResult.Value, expectedEventSequence: 5);
//
//         var aggregateResult = await CosmosDataStore.GetAggregate(streamId, aggregateId);
//
//         using (new AssertionScope())
//         {
//             aggregateResult.IsSuccess.Should().BeTrue();
//
//             aggregateResult.Value.Should().NotBeNull();
//
//             aggregateResult.Value.StreamId.Should().Be(streamId.Id);
//             aggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
//             aggregateResult.Value.Version.Should().Be(2);
//             aggregateResult.Value.LatestEventSequence.Should().Be(6);
//
//             aggregateResult.Value.Id.Should().Be(id);
//             aggregateResult.Value.Name.Should().Be("Updated Name");
//             aggregateResult.Value.Description.Should().Be("Updated Description");
//         }
//     }
//
//     [Fact]
//     public async Task GivenNewAggregateSaved_ThenAuditablePropertiesArePopulated()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//         var now = new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc);
//
//         var timeProvider = new FakeTimeProvider();
//         timeProvider.SetUtcNow(now);
//
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//         var aggregateEntity = await CosmosDataStore.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));
//         var eventEntity = await CosmosDataStore.Events.AsNoTracking().FirstOrDefaultAsync(a => a.StreamId == streamId.Id);
//
//         using (new AssertionScope())
//         {
//             aggregateEntity.Should().NotBeNull();
//             aggregateEntity.CreatedBy.Should().Be("TestUser");
//             aggregateEntity.CreatedDate.Should().Be(now);
//             aggregateEntity.UpdatedBy.Should().Be("TestUser");
//             aggregateEntity.UpdatedDate.Should().Be(now);
//
//             eventEntity.Should().NotBeNull();
//             eventEntity.CreatedBy.Should().Be("TestUser");
//             eventEntity.CreatedDate.Should().Be(now);
//         }
//     }
//
//     [Fact]
//     public async Task GivenAggregateUpdated_ThenAuditablePropertiesArePopulated()
//     {
//         var id = Guid.NewGuid().ToString();
//         var streamId = new TestStreamId(id);
//         var aggregateId = new TestAggregate1Id(id);
//         var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
//
//         var timeProvider = new FakeTimeProvider();
//
//         var createDate = new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc);
//         timeProvider.SetUtcNow(createDate);
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
//
//         var updateDate = new DateTime(2024, 6, 15, 18, 0, 0, DateTimeKind.Utc);
//         timeProvider.SetUtcNow(updateDate);
//         var aggregateToUpdateResult = await CosmosDataStore.GetAggregate(streamId, aggregateId);
//         aggregateToUpdateResult.Value!.Update("Updated Name", "Updated Description");
//         await CosmosDataStore.SaveAggregate(streamId, aggregateId, aggregateToUpdateResult.Value, expectedEventSequence: 1);
//
//         var aggregateEntity = await CosmosDataStore.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));
//         var eventEntity = await CosmosDataStore.Events.AsNoTracking().FirstOrDefaultAsync(a => a.StreamId == streamId.Id);
//
//         using (new AssertionScope())
//         {
//             aggregateEntity.Should().NotBeNull();
//             aggregateEntity.CreatedBy.Should().Be("TestUser");
//             aggregateEntity.CreatedDate.Should().Be(createDate);
//             aggregateEntity.UpdatedBy.Should().Be("TestUser");
//             aggregateEntity.UpdatedDate.Should().Be(updateDate);
//
//             eventEntity.Should().NotBeNull();
//             eventEntity.CreatedBy.Should().Be("TestUser");
//             eventEntity.CreatedDate.Should().Be(createDate);
//         }
//     }
// }
