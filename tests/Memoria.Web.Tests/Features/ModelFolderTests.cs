using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;
using Memoria.Results;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Folding one model's state out of its stream up to a sequence, without writing it anywhere. The
/// store is asked through reflection, because the model type is not known until someone uploads it,
/// so what is pinned here is that the right overload is closed and called with the sequence asked
/// for, and how what comes back is read — a model, the store's own failure, or a store that threw.
/// </summary>
public class ModelFolderTests
{
    private static readonly SampleStreamId Stream = new("sample-1");

    private static readonly SampleAggregateId AggregateId = new("abc-1");

    private static IDomainService AggregateStore(int upToSequence, Result<SampleAggregate> result)
    {
        var store = Substitute.For<IDomainService>();

        store.GetInMemoryAggregate<SampleAggregate>(
                Arg.Any<IStreamId>(),
                Arg.Any<IAggregateId<SampleAggregate>>(),
                upToSequence,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    [Fact]
    public async Task Hands_back_the_model_folded_up_to_the_sequence_asked_for()
    {
        var folded = new SampleAggregate();
        var store = AggregateStore(3, folded);

        var fold = await ModelFolder.Fold(store, typeof(SampleAggregate), Stream, AggregateId, 3);

        fold.Model.Should().BeSameAs(folded);
        fold.Error.Should().BeNull();
    }

    /// <summary>
    /// The sequence travels through as given: a fold up to three is not a fold up to the end, and
    /// the store's other two overloads — the whole stream, and up to a date — are not what was asked.
    /// </summary>
    [Fact]
    public async Task Asks_the_store_for_the_sequence_given_and_no_other()
    {
        var store = AggregateStore(3, new SampleAggregate());

        await ModelFolder.Fold(store, typeof(SampleAggregate), Stream, AggregateId, 3);

        await store.Received(1).GetInMemoryAggregate<SampleAggregate>(
            Stream, AggregateId, 3, Arg.Any<CancellationToken>());
        await store.DidNotReceive().GetInMemoryAggregate<SampleAggregate>(
            Arg.Any<IStreamId>(), Arg.Any<IAggregateId<SampleAggregate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong()
    {
        var store = AggregateStore(3,
            (Result<SampleAggregate>)new Failure(ErrorCode.Error, "Stream unreadable",
                "An event type could not be resolved."));

        var fold = await ModelFolder.Fold(store, typeof(SampleAggregate), Stream, AggregateId, 3);

        fold.Model.Should().BeNull();
        fold.Error.Should().Contain("Stream unreadable").And.Contain("An event type could not be resolved.");
    }

    [Fact]
    public async Task Reports_a_store_that_threw()
    {
        var store = Substitute.For<IDomainService>();

        store.GetInMemoryAggregate<SampleAggregate>(
                Arg.Any<IStreamId>(),
                Arg.Any<IAggregateId<SampleAggregate>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<Result<SampleAggregate>>>(_ => throw new InvalidOperationException("no connection"));

        var fold = await ModelFolder.Fold(store, typeof(SampleAggregate), Stream, AggregateId, 3);

        fold.Model.Should().BeNull();
        fold.Error.Should().Contain("no connection");
    }

    /// <summary>
    /// What a page hands over if the uploaded types were reloaded under it: an identifier that is
    /// not a streamed aggregate's. Said, and the store not asked.
    /// </summary>
    [Fact]
    public async Task Reports_an_identifier_that_does_not_name_a_streamed_aggregate()
    {
        var store = Substitute.For<IDomainService>();

        var fold = await ModelFolder.Fold(store, typeof(SampleAggregate), Stream, new object(), 3);

        fold.Model.Should().BeNull();
        fold.Error.Should().NotBeNullOrWhiteSpace();
        store.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_a_stream_that_is_not_one()
    {
        var store = Substitute.For<IDomainService>();

        var fold = await ModelFolder.Fold(store, typeof(SampleAggregate), new object(), AggregateId, 3);

        fold.Model.Should().BeNull();
        fold.Error.Should().NotBeNullOrWhiteSpace();
        store.ReceivedCalls().Should().BeEmpty();
    }

    // A read model is folded by the store's own projection method. Asking for it through the
    // aggregate's would not compile a closed method at all — the constraint is on IAggregateRoot —
    // so which one is called is what makes the projection page's compare tab work rather than a
    // detail of how. As with the refresh, the identifier says which.

    private static readonly SampleProjectionId ProjectionId = new("abc-1");

    private static IDomainService ProjectionStore(int upToSequence, Result<SampleProjection> result)
    {
        var store = Substitute.For<IDomainService>();

        store.GetInMemoryProjection<SampleProjection>(
                Arg.Any<IStreamId>(),
                Arg.Any<IProjectionId<SampleProjection>>(),
                upToSequence,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    [Fact]
    public async Task Folds_a_projection_through_the_stores_projection_method()
    {
        var folded = new SampleProjection();
        var store = ProjectionStore(3, folded);

        var fold = await ModelFolder.Fold(store, typeof(SampleProjection), Stream, ProjectionId, 3);

        fold.Model.Should().BeSameAs(folded);
        fold.Error.Should().BeNull();

        await store.Received(1).GetInMemoryProjection<SampleProjection>(
            Stream, ProjectionId, 3, Arg.Any<CancellationToken>());
        await store.DidNotReceive().GetInMemoryProjection<SampleProjection>(
            Arg.Any<IStreamId>(), Arg.Any<IProjectionId<SampleProjection>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong_about_a_projection()
    {
        var store = ProjectionStore(3,
            (Result<SampleProjection>)new Failure(ErrorCode.Error, "Stream unreadable",
                "An event type could not be resolved."));

        var fold = await ModelFolder.Fold(store, typeof(SampleProjection), Stream, ProjectionId, 3);

        fold.Model.Should().BeNull();
        fold.Error.Should().Contain("Stream unreadable");
    }
}
