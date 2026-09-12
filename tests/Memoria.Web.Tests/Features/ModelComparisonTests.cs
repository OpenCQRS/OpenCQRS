using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;
using Memoria.Results;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The compare tab's one question, asked of the store's reads and folds together: which two
/// sequences, folded to what, differing where. What is pinned is what the tab compares when the
/// address names nothing, that a pair is checked against the model's own last event before the
/// store is asked to fold anything, and that each way the store can decline is carried through as
/// the reason the tab has no table.
/// </summary>
public class ModelComparisonTests
{
    private static readonly SampleStreamId Stream = new("sample-1");

    private static readonly SampleCountingAggregateId AggregateId = new("abc-1");

    private static readonly StreamedIdentity Identity = new(
        typeof(SampleStreamId), Stream, typeof(SampleCountingAggregateId), AggregateId, Values: null);

    private static ComparisonRequest Request(string? from, string? to) =>
        new(typeof(SampleCountingAggregate), Identity, StreamId: "sample-1", EventTypes: null, from, to);

    private static IStreamedReads History(long? lastSequence)
    {
        var reads = Substitute.For<IStreamedReads>();

        var page = lastSequence switch
        {
            null => new StoredStreamEvents([], 0, 1, 1, "The store could not be reached."),
            0 => new StoredStreamEvents([], 0, 1, 1, null),
            _ => new StoredStreamEvents(
                [new StoredStreamEvent("sample-1", $"sample-1:{lastSequence}", Event(lastSequence.Value))],
                (int)lastSequence.Value, 1, (int)lastSequence.Value, null)
        };

        reads.Events(Arg.Any<StreamedEventFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(page));

        return reads;
    }

    private static StoredEvent Event(long position) =>
        new(position, "Sample:1", DateTimeOffset.UnixEpoch, "{}", [], null, []);

    private static IDomainService Folding(params (int UpToSequence, Result<SampleCountingAggregate> Result)[] folds)
    {
        var store = Substitute.For<IDomainService>();

        foreach (var (upToSequence, result) in folds)
        {
            store.GetInMemoryAggregate<SampleCountingAggregate>(
                    Arg.Any<IStreamId>(),
                    Arg.Any<IAggregateId<SampleCountingAggregate>>(),
                    upToSequence,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(result));
        }

        return store;
    }

    private static SampleCountingAggregate Counting(int count) => new() { Count = count };

    [Fact]
    public async Task Compares_the_last_event_against_the_state_before_it_when_nothing_is_asked_for()
    {
        var store = Folding((13, Counting(3)), (14, Counting(5)));

        var comparison = await ModelComparison.Of(History(14), store, Request(null, null));

        comparison.LastSequence.Should().Be(14);
        comparison.Range.Should().Be(new CompareRange(13, 14));
        comparison.Error.Should().BeNull();
        comparison.Rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Count", "int", "3", "5", Change.Changed, []));
    }

    /// <summary>
    /// Asked in the words of the events tab: newest first and one row, narrowed to what this model
    /// applies, so the last event found is this model's and not another's on a shared stream.
    /// </summary>
    [Fact]
    public async Task Reads_the_models_own_last_event_newest_first()
    {
        var reads = History(14);
        var request = Request("3", "7") with { EventTypes = ["Sample:1"] };

        await ModelComparison.Of(reads, Folding((3, Counting(1)), (7, Counting(2))), request);

        await reads.Received(1).Events(
            Arg.Is<StreamedEventFilter>(filter =>
                filter.StreamPattern == "sample-1" &&
                filter.Descending &&
                filter.Size == 1 &&
                filter.EventTypes!.SequenceEqual(new[] { "Sample:1" }) &&
                filter.Properties != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Folds_the_pair_asked_for()
    {
        var store = Folding((3, Counting(1)), (7, Counting(1)));

        var comparison = await ModelComparison.Of(History(14), store, Request("3", "7"));

        comparison.Range.Should().Be(new CompareRange(3, 7));
        comparison.Rows.Single().Change.Should().Be(Change.Unchanged);
    }

    [Fact]
    public async Task Has_nothing_to_compare_on_a_history_with_no_events()
    {
        var store = Folding();

        var comparison = await ModelComparison.Of(History(0), store, Request(null, null));

        comparison.LastSequence.Should().Be(0);
        comparison.Range.Should().BeNull();
        comparison.Error.Should().Contain("no events");
        store.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// A count that failed says nothing rather than nothing found: there is no last event to
    /// default to, but a pair given is still folded, since the store may well fold what it could
    /// not count.
    /// </summary>
    [Fact]
    public async Task Folds_a_pair_given_even_when_the_history_could_not_be_counted()
    {
        var store = Folding((3, Counting(1)), (7, Counting(2)));

        var asked = await ModelComparison.Of(History(null), store, Request("3", "7"));
        var unasked = await ModelComparison.Of(History(null), store, Request(null, null));

        asked.LastSequence.Should().BeNull();
        asked.Range.Should().Be(new CompareRange(3, 7));
        asked.Rows.Single().Change.Should().Be(Change.Changed);

        unasked.Range.Should().BeNull();
        unasked.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refuses_a_pair_past_the_last_event_without_asking_the_store_to_fold()
    {
        var store = Folding();

        var comparison = await ModelComparison.Of(History(14), store, Request("3", "15"));

        comparison.Range.Should().BeNull();
        comparison.Error.Should().Contain("14");
        store.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_a_fold_the_store_refused()
    {
        var store = Folding(
            (13, Counting(3)),
            (14, (Result<SampleCountingAggregate>)new Failure(ErrorCode.Error, "Stream unreadable", "Event 14 will not open.")));

        var comparison = await ModelComparison.Of(History(14), store, Request(null, null));

        comparison.Range.Should().Be(new CompareRange(13, 14));
        comparison.Error.Should().Contain("Stream unreadable");
        comparison.Rows.Should().BeEmpty();
    }

    /// <summary>
    /// Without a rebuilt stream and identifier there is nothing to fold through, and the panel says
    /// which half is missing — so nothing is read and nothing is folded.
    /// </summary>
    [Fact]
    public async Task Asks_nothing_when_the_identity_was_not_rebuilt()
    {
        var reads = History(14);
        var store = Folding();
        var request = Request(null, null) with { Identity = StreamedIdentity.Unknown };

        var comparison = await ModelComparison.Of(reads, store, request);

        comparison.Should().Be(ModelComparison.None);
        reads.ReceivedCalls().Should().BeEmpty();
        store.ReceivedCalls().Should().BeEmpty();
    }
}

/// <summary>An aggregate with one property to read, so a comparison has a row to show.</summary>
public class SampleCountingAggregate : AggregateRoot
{
    public int Count { get; set; }

    public override Type[]? EventTypeFilter => null;

    protected override bool Apply<T>(T @event) => false;
}

public class SampleCountingAggregateId(string id) : IAggregateId<SampleCountingAggregate>
{
    public string Id { get; } = id;

    public IDictionary<string, string>? EventPropertyFilter => null;
}
