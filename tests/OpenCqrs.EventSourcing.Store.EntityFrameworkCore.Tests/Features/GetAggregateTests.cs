﻿using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Streams;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Features;

public class GetAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAggregateExists_ThenAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        
        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            getAggregateResult.Value.Version.Should().Be(1);

            getAggregateResult.Value.Id.Should().Be(aggregate.Id);
            getAggregateResult.Value.Name.Should().Be(aggregate.Name);
            getAggregateResult.Value.Description.Should().Be(aggregate.Description);
        }
    }

    [Fact]
    public async Task GivenAggregateExists_WhenAggregateIsUpdated_ThenTheUpdatedAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var updatedAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);
        aggregate = updatedAggregateResult.Value!;
        aggregate.Update("Updated Name", "Updated Description");
        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 1);
        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            getAggregateResult.Value.Version.Should().Be(2);

            getAggregateResult.Value.Id.Should().Be(aggregate.Id);
            getAggregateResult.Value.Name.Should().Be("Updated Name");
            getAggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }

    [Fact]
    public async Task GivenNewAggregateSaved_WhenMultipleEventsAdded_ThenTheUpdatedAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");
        aggregate.Update("Updated Name", "Updated Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            getAggregateResult.Value.Version.Should().Be(2);

            getAggregateResult.Value.Id.Should().Be(aggregate.Id);
            getAggregateResult.Value.Name.Should().Be("Updated Name");
            getAggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenNoEventsAreStored_ThenFailureIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();
            getAggregateResult.Failure.Should().BeNull();
            getAggregateResult.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenEventsAreStoredButNotApplied_ThenNullIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var events = new IEvent[]
        {
            new SomethingHappenedEvent(Something: "Something")
        };
        await DomainService.SaveEvents(streamId, events, expectedEventSequence: 0);

        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId, ReadMode.SnapshotOrCreate);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();
            getAggregateResult.Failure.Should().BeNull();
            getAggregateResult.Value.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenEventsAreStoredAndApplied_ThenNewAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        var events = new IEvent[]
        {
            new TestAggregateCreatedEvent(id, "Test Name", "Test Description"),
            new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")
        };
        await DomainService.SaveEvents(streamId, events, expectedEventSequence: 0);

        var getAggregateResult = await DomainService.GetAggregate(streamId, aggregateId, readMode: ReadMode.SnapshotOrCreate);

        using (new AssertionScope())
        {
            getAggregateResult.IsSuccess.Should().BeTrue();

            getAggregateResult.Value.Should().NotBeNull();

            getAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            getAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            getAggregateResult.Value.Version.Should().Be(2);

            getAggregateResult.Value.Id.Should().Be(id);
            getAggregateResult.Value.Name.Should().Be("Updated Name");
            getAggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }

    [Fact]
    public async Task GivenEventsHandledByTheAggregateAreStoredSeparately_WhenApplyNewEventsIsRequested_ThenTheUpdatedAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await DomainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        var events = new IEvent[]
        {
            new TestAggregateUpdatedEvent(id, "Updated Name", "Updated Description")
        };
        await DomainService.SaveEvents(streamId, events, expectedEventSequence: 1);

        var updatedAggregateResult = await DomainService.GetAggregate(streamId, aggregateId, ReadMode.SnapshotWithNewEvents);

        using (new AssertionScope())
        {
            updatedAggregateResult.IsSuccess.Should().BeTrue();

            updatedAggregateResult.Value.Should().NotBeNull();

            updatedAggregateResult.Value.StreamId.Should().Be(streamId.Id);
            updatedAggregateResult.Value.AggregateId.Should().Be(aggregateId.ToStoreId());
            updatedAggregateResult.Value.Version.Should().Be(2);

            updatedAggregateResult.Value.Id.Should().Be(aggregate.Id);
            updatedAggregateResult.Value.Name.Should().Be("Updated Name");
            updatedAggregateResult.Value.Description.Should().Be("Updated Description");
        }
    }
}
