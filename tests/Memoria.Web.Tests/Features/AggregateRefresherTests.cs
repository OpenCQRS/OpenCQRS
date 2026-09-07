using FluentAssertions;
using Memoria.EventSourcing.Dcb;
using Memoria.Results;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Refreshing one aggregate's snapshot. The store is asked through reflection, because the aggregate
/// type is not known until someone uploads it, so what is pinned here is the unwrapping of what
/// comes back — including the difference between nothing to do and something going wrong.
/// </summary>
public class AggregateRefresherTests
{
    private static readonly SampleDcbAggregateId Id = new("abc-1");

    private static IDcbDomainService StoreReturning(Result<SampleDcbAggregate?> result)
    {
        var store = Substitute.For<IDcbDomainService>();

        store.UpdateAggregate<SampleDcbAggregate>(
                Arg.Any<IDcbAggregateId<SampleDcbAggregate>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        return store;
    }

    [Fact]
    public async Task Reports_a_snapshot_that_was_brought_up_to_date()
    {
        var store = StoreReturning(new SampleDcbAggregate());

        var refreshed = await AggregateRefresher.Refresh(store, typeof(SampleDcbAggregate), Id);

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
        var store = StoreReturning(new Success<SampleDcbAggregate?>(null));

        var refreshed = await AggregateRefresher.Refresh(store, typeof(SampleDcbAggregate), Id);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_what_the_store_said_went_wrong()
    {
        var store = StoreReturning(new Failure(ErrorCode.Error, "Boundary unreadable", "The tag head was missing."));

        var refreshed = await AggregateRefresher.Refresh(store, typeof(SampleDcbAggregate), Id);

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

        var refreshed = await AggregateRefresher.Refresh(store, typeof(SampleDcbAggregate), Id);

        refreshed.Refreshed.Should().BeFalse();
        refreshed.Error.Should().Contain("no connection");
    }
}
