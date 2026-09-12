using FluentAssertions;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Turning stored event rows into what the page shows — which of them the aggregate applies, which
/// page of them is being looked at, and what one row's payload holds. Which rows are inside a
/// boundary at all is the store's own query, and is covered against a real store rather than here.
/// </summary>
/// <remarks>
/// The event type bindings are process-wide, so these run alongside the other tests that rebuild
/// them rather than beside them.
/// </remarks>
[Collection(nameof(TypeBindingsCollection))]
public class BoundaryEventsTests : IDisposable
{
    private static readonly DateTimeOffset Written = new(2026, 5, 6, 11, 15, 0, TimeSpan.Zero);

    private readonly Dictionary<string, Type> _bindings = TypeBindings.EventTypeBindings;

    public BoundaryEventsTests() =>
        TypeBindings.EventTypeBindings = new Dictionary<string, Type>
        {
            { "SampleHappened:1", typeof(SampleHappenedEvent) }
        };

    public void Dispose()
    {
        TypeBindings.EventTypeBindings = _bindings;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Reads_the_event_the_payload_was_written_from()
    {
        var data = DomainSerializer.Current.Serialize(new SampleHappenedEvent("abc-1"));

        var read = BoundaryEvents.Read(position: 7, "SampleHappened:1", data, Written);

        read.Position.Should().Be(7);
        read.Type.Should().Be("SampleHappened:1");
        read.Written.Should().Be(Written);
        read.State.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Name = "Id", Value = "abc-1" });
        read.Error.Should().BeNull();
    }

    /// <summary>
    /// An event whose type was never uploaded, or was uploaded and then replaced. The row is still
    /// worth listing — position, type and date say something even when the payload cannot be read.
    /// </summary>
    [Fact]
    public void Lists_an_event_whose_type_is_not_registered()
    {
        var read = BoundaryEvents.Read(position: 7, "NeverUploaded:1", """{"Id":"abc-1"}""", Written);

        read.Position.Should().Be(7);
        read.Type.Should().Be("NeverUploaded:1");
        read.State.Should().BeEmpty();
        read.Error.Should().Contain("NeverUploaded:1");
    }

    /// <summary>
    /// The key is one string in the store and two facts on a page — the name a type is written
    /// under, and the version of that name — so a row can say each of them in its own column.
    /// </summary>
    [Fact]
    public void Reads_the_name_and_version_out_of_the_key_it_was_stored_under()
    {
        var read = BoundaryEvents.Read(position: 7, "SampleHappened:1", """{"Id":"abc-1"}""", Written);

        read.Name.Should().Be("SampleHappened");
        read.Version.Should().Be("1");
    }

    /// <summary>
    /// A key is written as name:version, so a name that contains a colon of its own still leaves
    /// the version as everything after the last one.
    /// </summary>
    [Fact]
    public void Takes_the_version_from_the_last_separator_in_the_key()
    {
        var read = BoundaryEvents.Read(position: 7, "Sample:Happened:2", "{}", Written);

        read.Name.Should().Be("Sample:Happened");
        read.Version.Should().Be("2");
    }

    /// <summary>
    /// A row is listed whatever its type string turns out to be, so one that carries no version at
    /// all is a name with nothing to say beside it rather than a row that cannot be drawn.
    /// </summary>
    [Fact]
    public void Reports_no_version_for_a_key_that_carries_none()
    {
        var read = BoundaryEvents.Read(position: 7, "SampleHappened", "{}", Written);

        read.Name.Should().Be("SampleHappened");
        read.Version.Should().BeNull();
    }

    [Fact]
    public void Reports_a_payload_that_cannot_be_read_back()
    {
        var read = BoundaryEvents.Read(position: 7, "SampleHappened:1", "{not json", Written);

        read.State.Should().BeEmpty();
        read.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Reports_a_payload_that_reads_back_as_nothing()
    {
        var read = BoundaryEvents.Read(position: 7, "SampleHappened:1", "null", Written);

        read.State.Should().BeEmpty();
        read.Error.Should().Contain("empty");
    }

    /// <summary>
    /// The payload as the log wrote it, kept beside the properties it was read into: the Json column
    /// shows the row's own text, and it has to survive whatever becomes of the read — a type nothing
    /// uploaded describes and a payload that will not open are exactly the rows worth looking at.
    /// </summary>
    [Fact]
    public void Keeps_the_payload_the_log_wrote()
    {
        BoundaryEvents.Read(position: 7, "SampleHappened:1", """{"Id":"abc-1"}""", Written)
            .Data.Should().Be("""{"Id":"abc-1"}""");

        BoundaryEvents.Read(position: 7, "NeverUploaded:1", """{"Id":"abc-1"}""", Written)
            .Data.Should().Be("""{"Id":"abc-1"}""");

        BoundaryEvents.Read(position: 7, "SampleHappened:1", "{not json", Written)
            .Data.Should().Be("{not json");
    }

    /// <summary>
    /// The filter is a model's own account of which events it applies, so it is read off the model
    /// rather than worked out from anything else.
    /// </summary>
    [Fact]
    public void Reads_the_event_types_the_aggregate_applies()
    {
        BoundaryEvents.AppliedBy(typeof(SampleDcbAggregate), loaded: null)
            .Should().BeEquivalentTo([typeof(SampleHappenedEvent)]);
    }

    /// <summary>
    /// A null filter is the model saying it applies everything, so nothing is narrowed.
    /// </summary>
    [Fact]
    public void Narrows_nothing_for_an_aggregate_that_applies_everything()
    {
        BoundaryEvents.AppliedBy(typeof(SampleAggregate), loaded: null).Should().BeNull();
    }

    /// <summary>
    /// The one already read is the real object, so it answers for itself rather than being built a
    /// second time.
    /// </summary>
    [Fact]
    public void Asks_the_aggregate_it_was_given_rather_than_a_fresh_one()
    {
        BoundaryEvents.AppliedBy(typeof(SampleAggregate), loaded: new SampleDcbAggregate())
            .Should().BeEquivalentTo([typeof(SampleHappenedEvent)]);
    }

    private static DcbEventEntity At(long position, DateTimeOffset written,
        string eventType = "SampleHappened:1") => new()
    {
        Position = position,
        EventType = eventType,
        Data = $$"""{"Id":"event-{{position}}"}""",
        CreatedDate = written
    };

    private static IReadOnlyList<DcbEventEntity> Rows(params long[] positions) =>
        positions.Select(position => At(position, Written.AddMinutes(position))).ToList();

    /// <summary>
    /// A boundary reads as a history, so it starts at the beginning until asked otherwise.
    /// </summary>
    [Fact]
    public void Orders_a_page_oldest_first()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3), eventType: null, text: null, descending: false, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(1, 2, 3);
        paged.Total.Should().Be(3);
        paged.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Turns_the_order_around_when_asked()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3), eventType: null, text: null, descending: true, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(3, 2, 1);
    }

    [Fact]
    public void Reads_only_the_page_asked_for()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3, 4, 5), eventType: null, text: null, descending: false, page: 2, size: 2);

        paged.Events.Select(stored => stored.Position).Should().Equal(3, 4);
        paged.Total.Should().Be(5);
        paged.Page.Should().Be(2);
        paged.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// A page number left over from a wider page size would otherwise skip past every row and show
    /// an empty table under a pager promising rows.
    /// </summary>
    [Fact]
    public void Brings_a_page_past_the_last_one_back_to_the_last()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3, 4, 5), eventType: null, text: null, descending: false, page: 99, size: 2);

        paged.Page.Should().Be(3);
        paged.Events.Select(stored => stored.Position).Should().Equal(5);
    }

    /// <summary>
    /// Two events appended in one transaction share a date. Position breaks the tie, so paging over
    /// them cannot show one row twice and drop another.
    /// </summary>
    [Fact]
    public void Orders_events_sharing_a_date_by_position()
    {
        IReadOnlyList<DcbEventEntity> together =
            [At(7, Written), At(5, Written), At(6, Written)];

        BoundaryEvents.Page(together, eventType: null, text: null, descending: false, page: 1, size: 10)
            .Events.Select(stored => stored.Position).Should().Equal(5, 6, 7);
    }

    /// <summary>
    /// The date is what the column sorts by, so a row appended later at a lower position still
    /// reads as later.
    /// </summary>
    [Fact]
    public void Orders_by_the_date_rather_than_the_position()
    {
        IReadOnlyList<DcbEventEntity> outOfStep =
            [At(1, Written.AddHours(2)), At(2, Written)];

        BoundaryEvents.Page(outOfStep, eventType: null, text: null, descending: false, page: 1, size: 10)
            .Events.Select(stored => stored.Position).Should().Equal(2, 1);
    }

    /// <summary>
    /// A history of one kind of event, which is how a boundary holding several is read for one of
    /// them. The key is what the log wrote the row under, so that is what it is matched on.
    /// </summary>
    [Fact]
    public void Narrows_a_page_to_one_event_type()
    {
        IReadOnlyList<DcbEventEntity> mixed =
        [
            At(1, Written, "SampleHappened:1"),
            At(2, Written.AddMinutes(1), "OtherHappened:1"),
            At(3, Written.AddMinutes(2), "SampleHappened:1")
        ];

        var paged = BoundaryEvents.Page(mixed, "SampleHappened:1", text: null,
            descending: false, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(1, 3);
        paged.Total.Should().Be(2);
    }

    /// <summary>
    /// The payload is matched as it was written, so the text reaches the property names as well as
    /// the values under them — a reader who knows only what a row carried should not have to know
    /// which property holds it.
    /// </summary>
    [Fact]
    public void Keeps_only_the_rows_whose_payload_carries_the_text()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3), eventType: null, text: "event-2",
            descending: false, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(2);
        paged.Total.Should().Be(1);
    }

    /// <summary>
    /// Typed rather than copied, so what was typed is matched however it was cased.
    /// </summary>
    [Fact]
    public void Matches_a_payload_whatever_the_case_it_was_typed_in()
    {
        BoundaryEvents.Page(Rows(1, 2, 3), eventType: null, text: "EVENT-2",
                descending: false, page: 1, size: 10)
            .Events.Select(stored => stored.Position).Should().Equal(2);
    }

    /// <summary>
    /// The payload is the only thing the box asks about, so a number is text like any other: it
    /// reaches the rows carrying it rather than the row numbered by it.
    /// </summary>
    [Fact]
    public void Asks_nothing_of_the_position_for_a_number_typed_into_the_box()
    {
        IReadOnlyList<DcbEventEntity> rows =
        [
            new() { Position = 1, EventType = "SampleHappened:1", Data = """{"Id":"12"}""",
                CreatedDate = Written },
            new() { Position = 12, EventType = "SampleHappened:1", Data = "{}",
                CreatedDate = Written.AddMinutes(1) }
        ];

        BoundaryEvents.Page(rows, eventType: null, text: "12", descending: false, page: 1, size: 10)
            .Events.Select(stored => stored.Position).Should().Equal(1);
    }

    /// <summary>
    /// A row is reached by its payload alone, so text nothing carries reaches nothing.
    /// </summary>
    [Fact]
    public void Keeps_no_row_whose_payload_does_not_carry_the_text()
    {
        BoundaryEvents.Page(Rows(1, 2, 3), eventType: null, text: "nothing-carries-this",
                descending: false, page: 1, size: 10)
            .Total.Should().Be(0);
    }

    /// <summary>
    /// The two narrow the same list, so a row has to answer both: the type it was written under and
    /// the text it carries.
    /// </summary>
    [Fact]
    public void Narrows_by_the_type_and_the_text_together()
    {
        IReadOnlyList<DcbEventEntity> mixed =
        [
            At(1, Written, "SampleHappened:1"),
            At(2, Written.AddMinutes(1), "OtherHappened:1")
        ];

        BoundaryEvents.Page(mixed, "SampleHappened:1", text: "event-2",
            descending: false, page: 1, size: 10).Total.Should().Be(0);
    }

    /// <summary>
    /// The pager counts what matched rather than what the boundary holds, so a narrowed table does
    /// not promise pages of rows the filter has taken away.
    /// </summary>
    [Fact]
    public void Pages_over_what_matched_rather_than_over_the_whole_boundary()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3, 4, 5), eventType: null, text: "event-1",
            descending: false, page: 1, size: 2);

        paged.Total.Should().Be(1);
        paged.TotalPages.Should().Be(1);
    }
}
