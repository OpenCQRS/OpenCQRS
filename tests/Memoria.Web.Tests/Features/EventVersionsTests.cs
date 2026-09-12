using FluentAssertions;
using Memoria.Web.Extensibility;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which version of a model each row on its events tab produced, for the compare column: the
/// model's versions count its own events from one, so a row's version is its place in the model's
/// history rather than its sequence in the stream, which on a shared stream counts other models'
/// events too.
/// <para>
/// What is pinned is when the store has to be asked. A table showing the whole history knows every
/// row's place from the page it is on, its order and the total; a table narrowed to some of the
/// history knows it for none of them, and asks how many of the model's events sit below each row.
/// </para>
/// </summary>
public class EventVersionsTests
{
    private static readonly StreamedEventFilter Model =
        new(StreamPattern: "customer:c-1", EventType: null, Text: null, Descending: false, Page: 1, Size: 10)
        {
            EventTypes = ["OrderPlaced:1"],
            Properties = new Dictionary<string, string> { ["orderId"] = "o-1" }
        };

    private static StoredEvent Event(long position) =>
        new(position, "OrderPlaced:1", DateTimeOffset.UnixEpoch, "{}", [], null, []);

    private static StoredStreamEvents Page(IReadOnlyList<long> positions, int total, int page, int size) =>
        new(positions.Select(position => new StoredStreamEvent("customer:c-1", $"customer:c-1:{position}", Event(position))).ToList(),
            total, page, Math.Max(1, (total + size - 1) / size), null);

    /// <summary>A store answering a count below each bound with the number given.</summary>
    private static IStreamedReads Counting(params (long Before, int Count)[] answers)
    {
        var reads = Substitute.For<IStreamedReads>();

        foreach (var (before, count) in answers)
        {
            reads.Events(Arg.Is<StreamedEventFilter>(filter => filter.BeforeSequence == before), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new StoredStreamEvents([], count, 1, 1, null)));
        }

        return reads;
    }

    /// <summary>The table read three rows to a page, which is what places a page in the history.</summary>
    private static readonly StreamedEventFilter ByThrees = Model with { Size = 3 };

    [Fact]
    public async Task Counts_the_rows_of_a_whole_history_from_the_page_they_are_on()
    {
        var reads = Counting();

        var versions = await EventVersions.Of(reads, ByThrees, Page([9, 10, 11], total: 8, page: 2, size: 3), descending: false, narrowed: false);

        versions.Should().Equal(new Dictionary<long, int> { [9] = 4, [10] = 5, [11] = 6 });
        reads.ReceivedCalls().Should().BeEmpty("the whole history is numbered by where the page sits in it");
    }

    /// <summary>
    /// The last page runs short, and its rows are still placed by a full page's worth for each page
    /// before it — not by however many this one holds.
    /// </summary>
    [Fact]
    public async Task Places_a_short_last_page_after_full_pages()
    {
        var versions = await EventVersions.Of(Counting(), ByThrees, Page([13, 14], total: 8, page: 3, size: 3), descending: false, narrowed: false);

        versions.Should().Equal(new Dictionary<long, int> { [13] = 7, [14] = 8 });
    }

    [Fact]
    public async Task Counts_downwards_when_the_newest_come_first()
    {
        var reads = Counting();

        var versions = await EventVersions.Of(reads, ByThrees, Page([14, 13, 12], total: 8, page: 1, size: 3), descending: true, narrowed: false);

        versions.Should().Equal(new Dictionary<long, int> { [14] = 8, [13] = 7, [12] = 6 });
    }

    [Fact]
    public async Task Counts_a_short_last_page_down_to_one_when_the_newest_come_first()
    {
        var versions = await EventVersions.Of(Counting(), ByThrees, Page([8, 7], total: 8, page: 3, size: 3), descending: true, narrowed: false);

        versions.Should().Equal(new Dictionary<long, int> { [8] = 2, [7] = 1 });
    }

    [Fact]
    public async Task Numbers_the_first_row_of_the_history_one()
    {
        var versions = await EventVersions.Of(Counting(), Model, Page([7, 8], total: 8, page: 1, size: 10), descending: false, narrowed: false);

        versions[7].Should().Be(1);
    }

    /// <summary>
    /// A narrowed table shows some of the history, so the page says nothing about where a row sits
    /// in the whole of it. Each row is placed by how many of the model's events come before it —
    /// the same stream, types and properties the table is read with, below the row's sequence.
    /// </summary>
    [Fact]
    public async Task Asks_how_many_of_the_models_events_sit_below_each_row_when_narrowed()
    {
        var reads = Counting((10, 3), (14, 7));

        var versions = await EventVersions.Of(reads, Model, Page([10, 14], total: 2, page: 1, size: 10), descending: false, narrowed: true);

        versions.Should().Equal(new Dictionary<long, int> { [10] = 4, [14] = 8 });

        await reads.Received(1).Events(
            Arg.Is<StreamedEventFilter>(filter =>
                filter.StreamPattern == "customer:c-1" &&
                filter.EventTypes!.SequenceEqual(new[] { "OrderPlaced:1" }) &&
                filter.Properties!["orderId"] == "o-1" &&
                filter.EventType == null &&
                filter.Text == null &&
                filter.Size == 1 &&
                filter.BeforeSequence == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_out_a_row_whose_count_the_store_refused()
    {
        var reads = Substitute.For<IStreamedReads>();

        reads.Events(Arg.Any<StreamedEventFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new StoredStreamEvents([], 0, 1, 1, "The store could not be reached.")));

        var versions = await EventVersions.Of(reads, Model, Page([10, 14], total: 2, page: 1, size: 10), descending: false, narrowed: true);

        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task Has_nothing_to_number_on_an_empty_page()
    {
        var reads = Counting();

        var versions = await EventVersions.Of(reads, Model, Page([], total: 0, page: 1, size: 10), descending: false, narrowed: false);

        versions.Should().BeEmpty();
        reads.ReceivedCalls().Should().BeEmpty();
    }
}
