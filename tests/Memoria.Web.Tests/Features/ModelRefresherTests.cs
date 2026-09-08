using FluentAssertions;
using Memoria.EventSourcing.Dcb;
using Memoria.Results;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Refreshing one model's snapshot. The store is asked through reflection, because the model type is
/// not known until someone uploads it, so what is pinned here is the unwrapping of what comes back —
/// including the difference between nothing to do and something going wrong — and that a projection
/// is refreshed as a projection rather than through the aggregate's method.
/// </summary>
public class ModelRefresherTests
{
    private static readonly SampleDcbAggregateId AggregateId = new("abc-1");

    private static readonly SampleDcbProjectionId ProjectionId = new("abc-1");

    private static IDcbDomainService AggregateStore(Result<SampleDcbAggregate?> result)
    {
        var store = Substitute.For<IDcbDomainService>();

        store.UpdateAggregate<SampleDcbAggregate>(
                Arg.Any<IDcbAggregateId<SampleDcbAggregate>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    private static IDcbDomainService ProjectionStore(Result<SampleDcbProjection?> result)
    {
        var store = Substitute.For<IDcbDomainService>();

        store.UpdateProjection<SampleDcbProjection>(
                Arg.Any<IDcbProjectionId<SampleDcbProjection>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    [Fact]
    public async Task Reports_a_snapshot_that_was_brought_up_to_date()
    {
        var store = AggregateStore(new SampleDcbAggregate());

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbAggregate), AggregateId);

        refreshed.Refreshed.Should().BeTrue();
        refreshed.Error.Should().BeNull();
    }

    /// <summary>
    /// No snapshot and no events this aggregate applies. That is an answer rather than a fault:
    /// there was nothing to bring up to date.
    /// </summary>
    [Fact]
    public async Task Reports_nothing_to_refresh_as_an_answer_rather_than_a_fault()
    {
        var store = AggregateStore(new Success<SampleDcbAggregate?>(null));

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbAggregate), AggregateId);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong()
    {
        var store = AggregateStore(
            (Result<SampleDcbAggregate?>)new Failure(ErrorCode.Error, "Boundary unreadable",
                "The tag head was missing."));

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbAggregate), AggregateId);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().Contain("Boundary unreadable").And.Contain("The tag head was missing.");
    }

    [Fact]
    public async Task Reports_a_store_that_threw()
    {
        var store = Substitute.For<IDcbDomainService>();

        store.UpdateAggregate<SampleDcbAggregate>(
                Arg.Any<IDcbAggregateId<SampleDcbAggregate>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<Result<SampleDcbAggregate?>>>(_ => throw new InvalidOperationException("no connection"));

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbAggregate), AggregateId);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().Contain("no connection");
    }

    /// <summary>
    /// A read model is refreshed by the store's own projection method. Asking for it through
    /// <c>UpdateAggregate</c> would not compile a closed method at all — the constraint is on
    /// <c>IDcbAggregateRoot</c> — so which one is called is what makes the projection page's button
    /// work rather than a detail of how.
    /// </summary>
    [Fact]
    public async Task Refreshes_a_projection_through_the_stores_projection_method()
    {
        var store = ProjectionStore(new SampleDcbProjection());

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbProjection), ProjectionId);

        refreshed.Refreshed.Should().BeTrue();
        refreshed.Error.Should().BeNull();

        await store.Received(1).UpdateProjection<SampleDcbProjection>(
            Arg.Any<IDcbProjectionId<SampleDcbProjection>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_a_projection_with_nothing_to_refresh_as_an_answer()
    {
        var store = ProjectionStore(new Success<SampleDcbProjection?>(null));

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbProjection), ProjectionId);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong_about_a_projection()
    {
        var store = ProjectionStore(
            (Result<SampleDcbProjection?>)new Failure(ErrorCode.Error, "Boundary unreadable",
                "The tag head was missing."));

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbProjection), ProjectionId);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().Contain("Boundary unreadable");
    }

    /// <summary>
    /// Neither an aggregate identifier nor a projection one, which is what a page would hand over if
    /// the uploaded types were reloaded under it. Said rather than silently doing nothing.
    /// </summary>
    [Fact]
    public async Task Reports_an_identifier_that_names_neither_kind_of_model()
    {
        var store = Substitute.For<IDcbDomainService>();

        var refreshed = await ModelRefresher.Refresh(store, typeof(SampleDcbAggregate), new object());

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().NotBeNullOrWhiteSpace();
    }
}
