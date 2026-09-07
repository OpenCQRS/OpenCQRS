using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The page reads the stored snapshot row itself, so what is left to pin here is turning one row
/// into what the page shows — the payload back into the aggregate it was written from, and the
/// row's own account of when it was stored. Finding the row is database work, and is covered
/// against a real store rather than here.
/// </summary>
public class AggregateReaderTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 2, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Updated = new(2026, 3, 4, 17, 45, 0, TimeSpan.Zero);

    private static DcbSnapshotEntity Row(string data) => new()
    {
        Id = "Aggregate:sample-1:1:digest",
        SnapshotKind = DcbSnapshotEntity.AggregateKind,
        StoreId = "sample-1:1",
        TagQuery = "sample:sample-1",
        ModelType = "SampleDcbAggregate:1",
        Version = 6,
        Data = data,
        CreatedDate = Created,
        UpdatedDate = Updated
    };

    [Fact]
    public void Rebuilds_the_aggregate_the_payload_was_written_from()
    {
        var read = AggregateReader.Read(typeof(SampleDcbAggregate), Row("""{"Name":"Kettle"}"""));

        read.Aggregate.Should().BeOfType<SampleDcbAggregate>().Which.Name.Should().Be("Kettle");
        read.Error.Should().BeNull();
    }

    /// <summary>
    /// The payload is written by <see cref="DomainSerializer"/> and has to be read back by it, so a
    /// round trip is the case that matters rather than a hand-written string alone.
    /// </summary>
    [Fact]
    public void Rebuilds_a_payload_this_serializer_wrote()
    {
        var data = DomainSerializer.Current.Serialize(new SampleDcbAggregate());

        AggregateReader.Read(typeof(SampleDcbAggregate), Row(data))
            .Aggregate.Should().BeOfType<SampleDcbAggregate>();
    }

    /// <summary>
    /// The version shown is the one the row was stored at, not one folded from events, so it comes
    /// off the row rather than off the payload.
    /// </summary>
    [Fact]
    public void Reports_the_version_and_dates_the_row_was_stored_with()
    {
        var read = AggregateReader.Read(typeof(SampleDcbAggregate), Row("""{"Name":"Kettle","Version":99}"""));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Version.Should().Be(6);
        read.Snapshot.Created.Should().Be(Created);
        read.Snapshot.Updated.Should().Be(Updated);
    }

    [Fact]
    public void Reports_a_payload_that_cannot_be_read_back()
    {
        var read = AggregateReader.Read(typeof(SampleDcbAggregate), Row("{not json"));

        read.Aggregate.Should().BeNull();
        read.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Reports_a_payload_that_reads_back_as_nothing()
    {
        var read = AggregateReader.Read(typeof(SampleDcbAggregate), Row("null"));

        read.Aggregate.Should().BeNull();
        read.Error.Should().Contain("empty");
    }

    /// <summary>
    /// When the payload is unreadable the row is still a fact: the page can say when something was
    /// stored even while it cannot say what.
    /// </summary>
    [Fact]
    public void Keeps_the_rows_dates_even_when_its_payload_is_unreadable()
    {
        var read = AggregateReader.Read(typeof(SampleDcbAggregate), Row("{not json"));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Updated.Should().Be(Updated);
    }
}
