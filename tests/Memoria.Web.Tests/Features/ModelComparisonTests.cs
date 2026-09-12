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
/// versions, folded up to which sequences, differing where. What is pinned is what the tab compares
/// when the address names nothing, that a pair of versions is checked against the model's last
/// before the store is asked to fold anything, how a version is turned into the sequence the fold
/// stops at, and that each way the store can decline is carried through as the reason the tab has
/// no table.
/// <para>
/// The model here has eight events in a stream of fourteen, at sequences 7 to 14: version one is
/// the fold up to 7, version eight the fold up to 14. A version is the model's own count, so the
/// six earlier sequences — another model's — do not move it.
/// </para>
/// </summary>
public class ModelComparisonTests
{
    private static readonly SampleStreamId Stream = new("sample-1");

    private static readonly SampleCountingAggregateId AggregateId = new("abc-1");

    private static readonly StreamedIdentity Identity = new(
        typeof(SampleStreamId), Stream, typeof(SampleCountingAggregateId), AggregateId, Values: null);

    private const int Versions = 8;

    /// <summary>The sequence a version of this model was folded up to.</summary>
    private static long SequenceOf(int version) => 6 + version;

    private static ComparisonRequest Request(string? from, string? to) =>
        new(typeof(SampleCountingAggregate), Identity, StreamId: "sample-1", EventTypes: null, from, to);

    private static StoredEvent Event(long position) =>
        new(position, "Sample:1", DateTimeOffset.UnixEpoch, "{}", [], null, []);

    private static StoredStreamEvents One(long position, int total) =>
        new([new StoredStreamEvent("sample-1", $"sample-1:{position}", Event(position))], total, 1, total, null);

    /// <summary>
    /// A history of the model's events, answering the count newest-first and the single event at
    /// any page of one ascending — which is how a version is turned into a sequence.
    /// </summary>
    private static IStreamedReads History(int? versions = Versions)
    {
        var reads = Substitute.For<IStreamedReads>();

        reads.Events(Arg.Any<StreamedEventFilter>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var filter = call.Arg<StreamedEventFilter>();

                if (versions is null)
                {
                    return Task.FromResult(new StoredStreamEvents([], 0, 1, 1, "The store could not be reached."));
                }

                if (versions == 0 || filter.Page > versions)
                {
                    return Task.FromResult(new StoredStreamEvents([], 0, 1, 1, null));
                }

                var position = filter.Descending ? SequenceOf(versions.Value) : SequenceOf(filter.Page);

                return Task.FromResult(One(position, versions.Value));
            });

        return reads;
    }

    private static IDomainService Folding(params (long UpToSequence, Result<SampleCountingAggregate> Result)[] folds)
    {
        var store = Substitute.For<IDomainService>();

        foreach (var (upToSequence, result) in folds)
        {
            store.GetInMemoryAggregate<SampleCountingAggregate>(
                    Arg.Any<IStreamId>(),
                    Arg.Any<IAggregateId<SampleCountingAggregate>>(),
                    (int)upToSequence,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(result));
        }

        return store;
    }

    private static SampleCountingAggregate Counting(int count) => new() { Count = count };

    [Fact]
    public async Task Compares_the_last_version_against_the_one_before_it_when_nothing_is_asked_for()
    {
        var store = Folding((13, Counting(3)), (14, Counting(5)));

        var comparison = await ModelComparison.Of(History(), store, Request(null, null));

        comparison.LastVersion.Should().Be(8);
        comparison.Range.Should().Be(new CompareRange(7, 8));
        comparison.Sequences.Should().Be((13L, 14L));
        comparison.Error.Should().BeNull();
        comparison.Rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Count", "int", "3", "5", Change.Changed, []));
    }

    /// <summary>
    /// The model's own history is what is counted: the same stream, types and properties the
    /// events tab is read with, so on a shared stream the count is this model's and not the
    /// stream's.
    /// </summary>
    [Fact]
    public async Task Counts_the_models_own_history()
    {
        var reads = History();
        var request = Request("3", "7") with { EventTypes = ["Sample:1"] };

        await ModelComparison.Of(reads, Folding((9, Counting(1)), (13, Counting(2))), request);

        await reads.Received().Events(
            Arg.Is<StreamedEventFilter>(filter =>
                filter.StreamPattern == "sample-1" &&
                filter.Descending &&
                filter.Size == 1 &&
                filter.EventTypes!.SequenceEqual(new[] { "Sample:1" }) &&
                filter.Properties != null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A version is the model's own count, so the fold for version three stops at the model's third
    /// event — sequence 9 here — and not at sequence 3, which is another model's.
    /// </summary>
    [Fact]
    public async Task Folds_each_version_up_to_the_sequence_of_the_models_own_event()
    {
        var store = Folding((9, Counting(1)), (13, Counting(1)));

        var comparison = await ModelComparison.Of(History(), store, Request("3", "7"));

        comparison.Range.Should().Be(new CompareRange(3, 7));
        comparison.Sequences.Should().Be((9L, 13L));
        comparison.Rows.Single().Change.Should().Be(Change.Unchanged);
    }

    /// <summary>
    /// Version zero is the model before anything happened to it, which is the fold up to sequence
    /// zero: no event to look up, and the store hands back a fresh model.
    /// </summary>
    [Fact]
    public async Task Folds_version_zero_up_to_sequence_zero()
    {
        var store = Folding((0, Counting(0)), (7, Counting(1)));

        var comparison = await ModelComparison.Of(History(), store, Request("0", "1"));

        comparison.Sequences.Should().Be((0L, 7L));
        comparison.Rows.Single().Change.Should().Be(Change.Changed);
    }

    [Fact]
    public async Task Has_nothing_to_compare_on_a_history_with_no_events()
    {
        var store = Folding();

        var comparison = await ModelComparison.Of(History(versions: 0), store, Request(null, null));

        comparison.LastVersion.Should().Be(0);
        comparison.Range.Should().BeNull();
        comparison.Error.Should().Contain("no events");
        store.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// A count that failed says nothing rather than nothing found: there is no last version to
    /// default to and nothing to bound a pair by, and with the history unreadable a version has no
    /// sequence to fold up to either — so the tab says why rather than folding the wrong thing.
    /// </summary>
    [Fact]
    public async Task Says_why_when_the_history_could_not_be_read()
    {
        var store = Folding();

        var asked = await ModelComparison.Of(History(versions: null), store, Request("3", "7"));
        var unasked = await ModelComparison.Of(History(versions: null), store, Request(null, null));

        asked.LastVersion.Should().BeNull();
        asked.Error.Should().NotBeNullOrWhiteSpace();
        unasked.Error.Should().NotBeNullOrWhiteSpace();
        store.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Refuses_a_version_past_the_last_without_asking_the_store_to_fold()
    {
        var store = Folding();

        var comparison = await ModelComparison.Of(History(), store, Request("3", "9"));

        comparison.Range.Should().BeNull();
        comparison.Error.Should().Contain("8");
        store.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_a_fold_the_store_refused()
    {
        var store = Folding(
            (13, Counting(3)),
            (14, (Result<SampleCountingAggregate>)new Failure(ErrorCode.Error, "Stream unreadable", "Event 14 will not open.")));

        var comparison = await ModelComparison.Of(History(), store, Request(null, null));

        comparison.Range.Should().Be(new CompareRange(7, 8));
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
        var reads = History();
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
