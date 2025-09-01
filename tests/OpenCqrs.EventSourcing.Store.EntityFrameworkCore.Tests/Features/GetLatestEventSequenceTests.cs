using FluentAssertions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Streams;
using Xunit;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Features;

public class GetLatestEventSequenceTests : TestBase
{
    [Fact]
    public async Task GivenNoDomainEventsSaved_TheLatestEventSequenceReturnedIsZero()
    {
        var id = Guid.NewGuid().ToString();
        var streamId = new TestStreamId(id);

        var latestEventSequence = await DomainService.GetLatestEventSequence(streamId);

        latestEventSequence.Value.Should().Be(0);
    }

    [Fact]
    public async Task GivenMultipleDomainEventsSaved_TheLatestEventSequenceIsReturned()
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
        var latestEventSequence = await dbContext.GetLatestEventSequence(streamId);

        latestEventSequence.Should().Be(4);
    }
}
