using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Working out which tag an identifier's value ends up in, so what already exists can be listed.
/// </summary>
public class IdentifierShapeTests
{
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
}
