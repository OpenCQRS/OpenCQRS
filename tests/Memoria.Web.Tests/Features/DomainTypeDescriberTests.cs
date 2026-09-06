using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class DomainTypeDescriberTests
{
    private static readonly Type[] Identifiers =
    [
        typeof(SampleDcbAggregateId), typeof(SampleAggregateId), typeof(SampleDcbProjectionId)
    ];

    private static DomainTypeDescription Describe(Type type) =>
        DomainTypeDescriber.Describe(type, Identifiers);

    [Fact]
    public void Reports_the_name_the_type_is_bound_under()
    {
        Describe(typeof(SampleDcbAggregate)).BindingKey.Should().Be("SampleDcbAggregate:1");
    }

    [Fact]
    public void Reports_no_binding_key_for_a_type_that_carries_no_attribute()
    {
        Describe(typeof(SampleDcbAggregateId)).BindingKey.Should().BeNull();
    }

    [Fact]
    public void Reports_the_assembly_the_type_came_from()
    {
        Describe(typeof(SampleDcbAggregate)).AssemblyName.Should().Be("Memoria.Web.Tests");
    }

    [Fact]
    public void Finds_the_identifiers_that_address_the_type()
    {
        Describe(typeof(SampleDcbAggregate)).Identifiers.Should().Equal(typeof(SampleDcbAggregateId));
    }

    [Fact]
    public void Leaves_out_identifiers_that_address_something_else()
    {
        Describe(typeof(SampleDcbAggregate)).Identifiers.Should().NotContain(typeof(SampleAggregateId));
    }

    [Fact]
    public void Finds_the_identifiers_of_a_streamed_aggregate_too()
    {
        Describe(typeof(SampleAggregate)).Identifiers.Should().Equal(typeof(SampleAggregateId));
    }

    [Fact]
    public void Reports_the_events_the_type_applies()
    {
        Describe(typeof(SampleDcbAggregate)).EventTypes.Should().Equal(typeof(SampleHappenedEvent));
    }

    [Fact]
    public void Reports_no_events_when_the_type_filters_none()
    {
        Describe(typeof(SampleAggregate)).EventTypes.Should().BeEmpty();
    }

    [Fact]
    public void Reports_the_properties_the_type_declares()
    {
        Describe(typeof(SampleDcbAggregate)).Properties
            .Should().Contain(property => property.Name == "Name" && property.TypeName == "string");
    }

    [Fact]
    public void Leaves_out_properties_inherited_from_the_framework()
    {
        Describe(typeof(SampleDcbAggregate)).Properties
            .Should().NotContain(property => property.Name == nameof(SampleDcbAggregate.Version));
    }

    /// <summary>
    /// A model overriding a framework property is describing the framework, not its own state, and
    /// the filter it overrides is already shown as the events it applies.
    /// </summary>
    [Fact]
    public void Leaves_out_properties_that_override_the_framework()
    {
        Describe(typeof(SampleDcbAggregate)).Properties
            .Should().NotContain(property => property.Name == "EventTypeFilter");
    }

    [Fact]
    public void Selects_the_type_asked_for_by_name()
    {
        DomainTypeDescriber.Select([typeof(SampleDcbAggregate)], typeof(SampleDcbAggregate).FullName)
            .Should().Be(typeof(SampleDcbAggregate));
    }

    [Fact]
    public void Selects_nothing_when_no_name_is_asked_for()
    {
        DomainTypeDescriber.Select([typeof(SampleDcbAggregate)], null).Should().BeNull();
    }

    [Fact]
    public void Selects_nothing_when_the_name_is_not_one_of_the_types()
    {
        DomainTypeDescriber.Select([typeof(SampleDcbAggregate)], "Contoso.Gone").Should().BeNull();
    }
}
