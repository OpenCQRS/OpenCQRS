using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.Results;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The store is asked through reflection, because the aggregate type is not known until someone
/// uploads it. These pin the unwrapping of what comes back.
/// </summary>
public class AggregateReaderTests
{
    private static readonly SampleDcbAggregateId Id = new("abc-1");

    private static IDcbDomainService StoreReturning(Result<SampleDcbAggregate?> result)
    {
        var store = Substitute.For<IDcbDomainService>();

        store.GetAggregate<SampleDcbAggregate>(
                Arg.Any<IDcbAggregateId<SampleDcbAggregate>>(),
                Arg.Any<ReadMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    [Fact]
    public async Task Hands_back_the_aggregate_the_store_found()
    {
        var found = new SampleDcbAggregate();
        var store = StoreReturning(found);

        var loaded = await AggregateReader.Load(store, typeof(SampleDcbAggregate), Id, ReadMode.SnapshotOnly);

        loaded.Aggregate.Should().BeSameAs(found);
        loaded.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_nothing_found_as_an_answer_rather_than_a_fault()
    {
        var store = StoreReturning(new Success<SampleDcbAggregate?>(null));

        var loaded = await AggregateReader.Load(store, typeof(SampleDcbAggregate), Id, ReadMode.SnapshotOnly);

        loaded.Aggregate.Should().BeNull();
        loaded.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong()
    {
        var store = StoreReturning(new Failure(ErrorCode.Error, "Boundary unreadable", "The tag head was missing."));

        var loaded = await AggregateReader.Load(store, typeof(SampleDcbAggregate), Id, ReadMode.SnapshotOnly);

        loaded.Aggregate.Should().BeNull();
        loaded.Error.Should().Contain("Boundary unreadable").And.Contain("The tag head was missing.");
    }

    [Fact]
    public async Task Reports_a_store_that_threw()
    {
        var store = Substitute.For<IDcbDomainService>();
        store.GetAggregate<SampleDcbAggregate>(
                Arg.Any<IDcbAggregateId<SampleDcbAggregate>>(),
                Arg.Any<ReadMode>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<Result<SampleDcbAggregate?>>>(_ => throw new InvalidOperationException("no connection"));

        var loaded = await AggregateReader.Load(store, typeof(SampleDcbAggregate), Id, ReadMode.SnapshotOnly);

        loaded.Aggregate.Should().BeNull();
        loaded.Error.Should().Contain("no connection");
    }
}
