using FluentAssertions;
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

public class GetInMemoryAggregateTests : TestBase
{
    [Fact]
    public async Task GivenAggregateDoesExist_ThenTheAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);
        var aggregate = new TestAggregate1(id, "Test Name", "Test Description");

        await using var dbContext = Shared.CreateTestDbContext();
        await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

        var getAggregateResult = await dbContext.GetInMemoryAggregate(streamId, aggregateId);

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
    public async Task GivenAggregateDoesNotExist_WhenEventsHandledByTheAggregateTypeAreStored_TheNewAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        await using var dbContext = Shared.CreateTestDbContext();

        dbContext.Add(new TestAggregateCreatedEvent(aggregateId.Id, "Test Name", "Test Description").ToEventEntity(streamId, sequence: 1));
        dbContext.Add(new TestAggregateUpdatedEvent(aggregateId.Id, "Updated Name", "Updated Description").ToEventEntity(streamId, sequence: 2));
        await dbContext.SaveChangesAsync();

        var aggregateResult = await dbContext.GetInMemoryAggregate(streamId, aggregateId);
        var aggregateEntity = await dbContext.Aggregates.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aggregateId.ToIdWithTypeVersion(1));

        using (new AssertionScope())
        {
            aggregateResult.IsSuccess.Should().BeTrue();

            aggregateResult.Value.Should().NotBeNull();
            aggregateResult.Value.StreamId.Should().Be(streamId.Id);
            aggregateResult.Value.AggregateId.Should().Be(aggregateId.ToIdWithTypeVersion(1));
            aggregateResult.Value.Version.Should().Be(2);
            aggregateResult.Value.Id.Should().Be(aggregateId.Id);
            aggregateResult.Value.Name.Should().Be("Updated Name");
            aggregateResult.Value.Description.Should().Be("Updated Description");

            aggregateEntity.Should().BeNull();
        }
    }

    [Fact]
    public async Task GivenAggregateDoesNotExist_WhenNoEventsAreStored_TheDefaultAggregateIsReturned()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);
        var aggregateId = new TestAggregate1Id(id);

        await using var dbContext = Shared.CreateTestDbContext();

        var result = await dbContext.GetInMemoryAggregate(streamId, aggregateId);

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

        await using var dbContext = Shared.CreateTestDbContext();
        dbContext.Add(new SomethingHappenedEvent(Something: "Something").ToEventEntity(streamId, sequence: 1));
        await dbContext.SaveChangesAsync();

        var result = await dbContext.GetInMemoryAggregate(streamId, aggregateId);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Failure.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Version.Should().Be(0);
        }
    }
}
