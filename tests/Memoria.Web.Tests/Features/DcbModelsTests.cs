using FluentAssertions;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The one place that says how the two DCB models differ. Worth pinning because every difference
/// here is silent when it is wrong: a projection page reading the aggregate discriminator lists
/// aggregates under a projection's name, and reading the wrong attribute for a binding key finds no
/// snapshot at all rather than saying why.
/// </summary>
public class DcbModelsTests
{
    [Fact]
    public void Names_each_kind_as_the_snapshot_row_discriminates_them()
    {
        DcbModelKind.Aggregate.SnapshotKind().Should().Be(DcbSnapshotEntity.AggregateKind);
        DcbModelKind.Projection.SnapshotKind().Should().Be(DcbSnapshotEntity.ProjectionKind);
    }

    /// <summary>
    /// A projection's key comes off its <c>[ProjectionType]</c> and an aggregate's off its
    /// <c>[AggregateType]</c>, so a model carrying only the other one is a failure rather than a
    /// key that finds nothing.
    /// </summary>
    [Fact]
    public void Reads_each_kinds_binding_key_off_its_own_attribute()
    {
        DcbModelKind.Aggregate.BindingKey(typeof(SampleDcbAggregate)).Should().Be("SampleDcbAggregate:1");
        DcbModelKind.Projection.BindingKey(typeof(SampleDcbProjection)).Should().Be("SampleDcbProjection:1");
    }

    [Fact]
    public void Takes_each_kind_from_its_own_list_of_the_catalogue()
    {
        var catalogue = new DomainTypeCatalogue
        {
            DcbAggregates = [typeof(SampleDcbAggregate)],
            DcbAggregateIds = [typeof(SampleDcbAggregateId)],
            DcbProjections = [typeof(SampleDcbProjection)],
            DcbProjectionIds = [typeof(SampleDcbProjectionId)]
        };

        catalogue.Models(DcbModelKind.Aggregate).Should().Equal(typeof(SampleDcbAggregate));
        catalogue.Identifiers(DcbModelKind.Aggregate).Should().Equal(typeof(SampleDcbAggregateId));
        catalogue.Models(DcbModelKind.Projection).Should().Equal(typeof(SampleDcbProjection));
        catalogue.Identifiers(DcbModelKind.Projection).Should().Equal(typeof(SampleDcbProjectionId));
    }

    /// <summary>
    /// Both kinds of identifier carry a boundary, and the pages want it without caring which they
    /// were handed.
    /// </summary>
    [Fact]
    public void Reads_the_boundary_off_either_kind_of_identifier()
    {
        var addressedByAggregateId = DcbModels.BoundaryOf(new SampleDcbAggregateId("abc-1"));
        var addressedByProjectionId = DcbModels.BoundaryOf(new SampleDcbProjectionId("abc-1"));

        addressedByAggregateId.Should().NotBeNull();
        addressedByAggregateId!.ToString().Should().Contain("sample:abc-1");

        addressedByProjectionId.Should().NotBeNull();
        addressedByProjectionId!.ToString().Should().Contain("sample:abc-1");
    }

    [Fact]
    public void Reads_no_boundary_off_something_that_is_not_an_identifier()
    {
        DcbModels.BoundaryOf(new object()).Should().BeNull();
        DcbModels.BoundaryOf(null).Should().BeNull();
    }
}
