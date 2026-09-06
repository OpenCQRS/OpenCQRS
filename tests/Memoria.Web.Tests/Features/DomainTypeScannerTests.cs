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
