using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Working out which tag an identifier's value ends up in, so what already exists can be listed.
/// </summary>
[Collection(nameof(TypeBindingsCollection))]
public class IdentifierShapeTests
{
    /// <summary>
    /// Probing means building an identifier and reading the boundary it produces, and the answer
    /// never changes for a given type — so it is worked out once.
    /// </summary>
    [Fact]
    public void Works_a_shape_out_once_and_keeps_it()
    {
        var first = IdentifierShape.Of(typeof(SampleTwoPartId));

        IdentifierShape.Of(typeof(SampleTwoPartId)).Should().BeSameAs(first);
    }

    /// <summary>
    /// An upload replaces the assembly its identifiers came from, so what was worked out about the
    /// old ones has to go with them.
    /// </summary>
    [Fact]
    public void Works_it_out_again_after_the_types_are_reloaded()
    {
        var first = IdentifierShape.Of(typeof(SampleTwoPartId));

        IdentifierShape.Forget();

        IdentifierShape.Of(typeof(SampleTwoPartId)).Should().NotBeSameAs(first);
    }

    [Fact]
    public void Keeps_the_absence_of_a_shape_as_well()
    {
        IdentifierShape.Of(typeof(SampleUnmappedId)).Should().BeNull();
        IdentifierShape.Of(typeof(SampleUnmappedId)).Should().BeNull();
    }

    [Fact]
    public void Reads_the_tag_one_value_is_written_into()
    {
        var shape = IdentifierShape.Of(typeof(SampleDcbAggregateId));

        shape!.Slots.Should().ContainSingle();
        shape.Slots[0].TagKey.Should().Be("sample");
        shape.Slots[0].ParameterName.Should().Be("id");
    }

    [Fact]
    public void Reads_a_tag_for_each_value_the_boundary_uses()
    {
        var shape = IdentifierShape.Of(typeof(SampleTwoPartId));

        shape!.Slots.Select(slot => (slot.TagKey, slot.ParameterName))
            .Should().BeEquivalentTo([("sample", "id"), ("label", "label")]);
    }

    /// <summary>
    /// A value the boundary never mentions cannot be recovered from the tags, so nothing can be
    /// listed and the page has to fall back to asking.
    /// </summary>
    [Fact]
    public void Reports_no_shape_when_a_value_reaches_no_tag()
    {
        IdentifierShape.Of(typeof(SampleUnmappedId)).Should().BeNull();
    }

    [Fact]
    public void Reports_no_shape_for_a_type_that_is_not_an_identifier()
    {
        IdentifierShape.Of(typeof(SampleDcbAggregate)).Should().BeNull();
    }

    [Fact]
    public void Turns_the_tags_of_one_instance_back_into_its_values()
    {
        var shape = IdentifierShape.Of(typeof(SampleTwoPartId))!;

        shape.ValuesFrom(["sample:abc", "label:widget", "unrelated:x"])
            .Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["id"] = "abc", ["label"] = "widget"
            });
    }

    [Fact]
    public void Recovers_nothing_when_a_tag_it_needs_is_absent()
    {
        var shape = IdentifierShape.Of(typeof(SampleTwoPartId))!;

        shape.ValuesFrom(["sample:abc"]).Should().BeNull();
    }

    /// <summary>
    /// The pattern a stored boundary of this shape matches, with a wildcard where each value goes.
    /// It is the identifier's own canonical rendering with the probe values taken out, so whatever
    /// shape the boundary really has is the shape that is matched.
    /// </summary>
    [Fact]
    public void Renders_the_pattern_a_stored_boundary_matches()
    {
        IdentifierShape.Of(typeof(SampleDcbAggregateId))!.BoundaryPattern.Should().Be("sample:%");
    }

    [Fact]
    public void Renders_a_wildcard_for_each_value_of_a_wider_boundary()
    {
        IdentifierShape.Of(typeof(SampleTwoPartId))!.BoundaryPattern.Should().Be("label:%,sample:%");
    }

    [Fact]
    public void Reads_the_values_back_out_of_a_stored_boundary()
    {
        var shape = IdentifierShape.Of(typeof(SampleTwoPartId))!;

        shape.ValuesFromBoundary("label:widget,sample:abc")
            .Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["id"] = "abc", ["label"] = "widget"
            });
    }

    [Fact]
    public void Reads_the_values_out_of_a_single_tag_boundary()
    {
        IdentifierShape.Of(typeof(SampleDcbAggregateId))!.ValuesFromBoundary("sample:abc")
            .Should().BeEquivalentTo(new Dictionary<string, string> { ["id"] = "abc" });
    }
}
