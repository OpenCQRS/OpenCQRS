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

    private static DcbEventEntity At(long position, DateTimeOffset written) => new()
    {
        Position = position,
        EventType = "SampleHappened:1",
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
        var paged = BoundaryEvents.Page(Rows(1, 2, 3), descending: false, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(1, 2, 3);
        paged.Total.Should().Be(3);
        paged.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Turns_the_order_around_when_asked()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3), descending: true, page: 1, size: 10);

        paged.Events.Select(stored => stored.Position).Should().Equal(3, 2, 1);
    }

    [Fact]
    public void Reads_only_the_page_asked_for()
    {
        var paged = BoundaryEvents.Page(Rows(1, 2, 3, 4, 5), descending: false, page: 2, size: 2);

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
        var paged = BoundaryEvents.Page(Rows(1, 2, 3, 4, 5), descending: false, page: 99, size: 2);

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

        BoundaryEvents.Page(together, descending: false, page: 1, size: 10)
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

        BoundaryEvents.Page(outOfStep, descending: false, page: 1, size: 10)
            .Events.Select(stored => stored.Position).Should().Equal(2, 1);
    }
}
