using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The page reads the stored snapshot row itself, so what is left to pin here is turning one row
/// into what the page shows — the payload back into the model it was written from, and the row's
/// own account of when it was stored. A row reads back the same way whichever kind of model wrote
/// it, which is why there is one reader for both. Finding the row is database work, and is covered
/// against a real store rather than here.
/// </summary>
public class ModelReaderTests
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
        LatestPosition = 412,
        Data = data,
        CreatedDate = Created,
        CreatedBy = "importer",
        UpdatedDate = Updated,
        UpdatedBy = "refresher"
    };

    [Fact]
    public void Rebuilds_the_model_the_payload_was_written_from()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("""{"Name":"Kettle"}"""));

        read.Model.Should().BeOfType<SampleDcbAggregate>().Which.Name.Should().Be("Kettle");
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

        ModelReader.Read(typeof(SampleDcbAggregate), Row(data))
            .Model.Should().BeOfType<SampleDcbAggregate>();
    }

    /// <summary>
    /// The version shown is the one the row was stored at, not one folded from events, so it comes
    /// off the row rather than off the payload.
    /// </summary>
    [Fact]
    public void Reports_the_version_and_dates_the_row_was_stored_with()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("""{"Name":"Kettle","Version":99}"""));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Version.Should().Be(6);
        read.Snapshot.Created.Should().Be(Created);
        read.Snapshot.Updated.Should().Be(Updated);
    }

    /// <summary>
    /// The rest of the row's own account of the write: what it was stored as, how far through the
    /// log the fold reached, and who each write is attributed to. None of it is in the payload.
    /// </summary>
    [Fact]
    public void Reports_what_the_row_says_about_the_write_itself()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("""{"Name":"Kettle"}"""));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.ModelType.Should().Be("SampleDcbAggregate:1");
        read.Snapshot.LatestPosition.Should().Be(412);
        read.Snapshot.CreatedBy.Should().Be("importer");
        read.Snapshot.UpdatedBy.Should().Be("refresher");
    }

    /// <summary>
    /// Audit is a store concern the application may leave switched off, so an unattributed row is
    /// an ordinary row rather than a broken one.
    /// </summary>
    [Fact]
    public void Reports_no_author_for_a_row_that_was_never_attributed()
    {
        var row = Row("""{"Name":"Kettle"}""");
        row.CreatedBy = null;
        row.UpdatedBy = null;

        var read = ModelReader.Read(typeof(SampleDcbAggregate), row);

        read.Snapshot!.CreatedBy.Should().BeNull();
        read.Snapshot.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void Reports_a_payload_that_cannot_be_read_back()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("{not json"));

        read.Model.Should().BeNull();
        read.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Reports_a_payload_that_reads_back_as_nothing()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("null"));

        read.Model.Should().BeNull();
        read.Error.Should().Contain("empty");
    }

    /// <summary>
    /// When the payload is unreadable the row is still a fact: the page can say when something was
    /// stored even while it cannot say what.
    /// </summary>
    [Fact]
    public void Keeps_the_rows_dates_even_when_its_payload_is_unreadable()
    {
        var read = ModelReader.Read(typeof(SampleDcbAggregate), Row("{not json"));

        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Updated.Should().Be(Updated);
    }
}
