using Memoria.EventSourcing.Domain;
using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class DomainTypeScannerTests
{
    private static DomainTypeCatalogue Scan() =>
        DomainTypeScanner.Scan([typeof(SampleAggregate).Assembly]);

    [Fact]
    public void Finds_streamed_aggregates()
    {
        Scan().StreamedAggregates.Should().Contain(typeof(SampleAggregate));
    }

    [Fact]
    public void Finds_streamed_aggregate_ids()
    {
        Scan().StreamedAggregateIds.Should().Contain(typeof(SampleAggregateId));
    }

    [Fact]
    public void Finds_streamed_streams()
    {
        Scan().StreamedStreamIds.Should().Contain(typeof(SampleStreamId));
    }

    /// <summary>
    /// A stream is not an aggregate's identifier: several aggregates may be folded from one stream,
    /// so the two are separate contracts and the page that counts them counts them apart.
    /// </summary>
    [Fact]
    public void Does_not_report_an_aggregate_id_as_a_stream()
    {
        Scan().StreamedStreamIds.Should().NotContain(typeof(SampleAggregateId));
    }

    [Fact]
    public void Finds_streamed_projections()
    {
        Scan().StreamedProjections.Should().Contain(typeof(SampleProjection));
    }

    [Fact]
    public void Finds_streamed_projection_ids()
    {
        Scan().StreamedProjectionIds.Should().Contain(typeof(SampleProjectionId));
    }

    [Fact]
    public void Finds_dcb_aggregates()
    {
        Scan().DcbAggregates.Should().Contain(typeof(SampleDcbAggregate));
    }

    [Fact]
    public void Finds_dcb_aggregate_ids()
    {
        Scan().DcbAggregateIds.Should().Contain(typeof(SampleDcbAggregateId));
    }

    [Fact]
    public void Finds_dcb_projections()
    {
        Scan().DcbProjections.Should().Contain(typeof(SampleDcbProjection));
    }

    [Fact]
    public void Finds_dcb_projection_ids()
    {
        Scan().DcbProjectionIds.Should().Contain(typeof(SampleDcbProjectionId));
    }

    [Fact]
    public void Finds_events()
    {
        Scan().Events.Should().Contain(typeof(SampleHappenedEvent));
    }

    /// <summary>
    /// The events found, as the pages that list them by consistency model read them. Two, so which
    /// of them a filter names is a question with an answer.
    /// </summary>
    private static readonly IReadOnlyList<Type> Found =
        [typeof(SampleHappenedEvent), typeof(SampleCarriedEvent)];

    [Fact]
    public void Applies_only_the_events_a_models_filter_names()
    {
        DomainTypeScanner.AppliedBy([typeof(SampleDcbAggregate)], Found)
            .Should().Equal(typeof(SampleHappenedEvent));
    }

    /// <summary>
    /// One model applying an event is enough for the model as a whole to apply it, so what comes
    /// back is the union of the filters and not the last one read.
    /// </summary>
    [Fact]
    public void Applies_the_events_of_every_model_together()
    {
        DomainTypeScanner.AppliedBy(
                [typeof(SampleDcbAggregate), typeof(SampleCarryingDcbAggregate)], Found)
            .Should().Equal(typeof(SampleHappenedEvent), typeof(SampleCarriedEvent));
    }

    /// <summary>
    /// <see cref="SampleDcbProjection"/> declares no filter, which means it applies whatever its
    /// boundary hands it — so every event found is one that model can apply, however narrow the
    /// filters beside it are.
    /// </summary>
    [Fact]
    public void Applies_every_event_when_a_model_filters_none()
    {
        DomainTypeScanner.AppliedBy(
                [typeof(SampleDcbAggregate), typeof(SampleDcbProjection)], Found)
            .Should().Equal(Found);
    }

    [Fact]
    public void Applies_nothing_when_there_is_no_model_to_apply_it()
    {
        DomainTypeScanner.AppliedBy([], Found).Should().BeEmpty();
    }

    /// <summary>
    /// A filter may name an event that was never uploaded. It cannot be listed, so it is not.
    /// </summary>
    [Fact]
    public void Leaves_out_an_event_the_assemblies_do_not_declare()
    {
        DomainTypeScanner.AppliedBy([typeof(SampleDcbAggregate)], [typeof(SampleCarriedEvent)])
            .Should().BeEmpty();
    }

    /// <summary>
    /// Both models in this assembly have a member that filters nothing — <c>SampleAggregate</c> and
    /// <see cref="SampleDcbProjection"/> — so each ends up applying everything that was found.
    /// </summary>
    [Fact]
    public void Reports_the_events_each_consistency_model_applies()
    {
        var scanned = Scan();

        scanned.DcbEvents.Should().Equal(scanned.Events);
        scanned.StreamedEvents.Should().Equal(scanned.Events);
    }

    [Fact]
    public void Does_not_report_an_aggregate_as_a_projection()
    {
        Scan().StreamedProjections.Should().NotContain(typeof(SampleAggregate));
    }

    [Fact]
    public void Does_not_report_a_dcb_aggregate_among_the_streamed_ones()
    {
        Scan().StreamedAggregates.Should().NotContain(typeof(SampleDcbAggregate));
    }

    [Fact]
    public void Excludes_abstract_types()
    {
        Scan().StreamedAggregates.Should().NotContain(typeof(AggregateRoot));
    }
}
