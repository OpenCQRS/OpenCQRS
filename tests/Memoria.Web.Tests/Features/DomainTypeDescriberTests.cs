using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class DomainTypeDescriberTests
{
    private static readonly Type[] Identifiers =
    [
        typeof(SampleDcbAggregateId), typeof(SampleTwoPartId), typeof(SampleUnmappedId),
        typeof(SampleAggregateId), typeof(SampleDcbProjectionId)
    ];

    private static DomainTypeDescription Describe(Type type) =>
        DomainTypeDescriber.Describe(type, Identifiers);

    /// <summary>
    /// The name and the version are read apart rather than as one key, because a page shows them
    /// as two facts about the type — the key is what the store writes them as.
    /// </summary>
    [Fact]
    public void Reports_the_name_and_version_the_type_is_bound_under()
    {
        var binding = Describe(typeof(SampleDcbAggregate)).Binding;

        binding.Should().NotBeNull();
        binding!.Name.Should().Be("SampleDcbAggregate");
        binding.Version.Should().Be(1);
        binding.Key.Should().Be("SampleDcbAggregate:1");
    }

    [Fact]
    public void Reports_no_binding_for_a_type_that_carries_no_attribute()
    {
        Describe(typeof(SampleDcbAggregateId)).Binding.Should().BeNull();
    }

    /// <summary>
    /// Reachable for a bare type, so a list can label its rows by what they are bound as without
    /// describing each one in full.
    /// </summary>
    [Fact]
    public void Reads_the_binding_of_a_type_on_its_own()
    {
        DomainTypeDescriber.BindingOf(typeof(SampleDcbAggregate))!.Key
            .Should().Be("SampleDcbAggregate:1");

        DomainTypeDescriber.BindingOf(typeof(SampleDcbAggregateId)).Should().BeNull();
    }

    /// <summary>
    /// Projections and events carry their own attributes, and are bound the same way.
    /// </summary>
    [Fact]
    public void Reads_the_binding_of_a_projection()
    {
        DomainTypeDescriber.BindingOf(typeof(SampleDcbProjection))!.Key
            .Should().Be("SampleDcbProjection:1");
    }

    [Fact]
    public void Reports_the_assembly_the_type_came_from()
    {
        Describe(typeof(SampleDcbAggregate)).AssemblyName.Should().Be("Memoria.Web.Tests");
    }

    [Fact]
    public void Finds_the_identifiers_that_address_the_type()
    {
        Describe(typeof(SampleDcbAggregate)).Identifiers.Should().BeEquivalentTo(
            new[] { typeof(SampleDcbAggregateId), typeof(SampleTwoPartId), typeof(SampleUnmappedId) });
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

    /// <summary>
    /// Reachable for a bare type, for the same reason the binding is: the index lists every
    /// aggregate, and describing each one in full constructs it to read its event filter.
    /// </summary>
    [Fact]
    public void Finds_the_identifiers_of_a_type_on_its_own()
    {
        DomainTypeDescriber.IdentifiersOf(typeof(SampleDcbAggregate), Identifiers)
            .Should().BeEquivalentTo(
                new[] { typeof(SampleDcbAggregateId), typeof(SampleTwoPartId), typeof(SampleUnmappedId) });
    }

    [Fact]
    public void Finds_no_identifiers_for_a_type_nothing_addresses()
    {
        DomainTypeDescriber.IdentifiersOf(typeof(SampleHappenedEvent), Identifiers).Should().BeEmpty();
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

    /// <summary>
    /// Reachable for a type that is neither an aggregate nor a projection — an event carries state
    /// the same way, and the aggregates page shows what each event it applies is carrying.
    /// </summary>
    [Fact]
    public void Reads_the_properties_of_a_type_that_is_not_a_model()
    {
        DomainTypeDescriber.PropertiesOf(typeof(SampleHappenedEvent))
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new DomainProperty("Id", "string"));
    }

    /// <summary>
    /// A property holding a value of the domain's own says nothing on its own — the shape is inside
    /// it, and the events tab is where someone goes to find out what an event carries.
    /// </summary>
    [Fact]
    public void Reads_the_properties_of_a_property_holding_a_value_of_its_own()
    {
        Carried("Measurement").Children.Select(child => child.Name)
            .Should().Equal("Height", "Width");
    }

    /// <summary>
    /// A list is described by what it holds. <c>IReadOnlyList&lt;SampleLabel&gt;</c> is already in
    /// the type column, so unfolding the list itself would repeat it and show nothing.
    /// </summary>
    [Fact]
    public void Reads_the_properties_of_what_a_list_holds()
    {
        Carried("Labels").Children.Select(child => child.Name).Should().Equal("Size", "Text");
    }

    [Fact]
    public void Unfolds_a_value_held_inside_another_value()
    {
        Carried("Labels").Children.Single(child => child.Name == "Size")
            .Children.Select(child => child.Name).Should().Equal("Height", "Width");
    }

    [Fact]
    public void Reads_nothing_beneath_a_plain_value()
    {
        Carried("Id").Children.Should().BeEmpty();
    }

    [Fact]
    public void Reads_nothing_beneath_a_list_of_plain_values()
    {
        Carried("Notes").Children.Should().BeEmpty();
    }

    /// <summary>
    /// An enum has no state to unfold, and its members are not properties of anything.
    /// </summary>
    [Fact]
    public void Reads_nothing_beneath_an_enum()
    {
        Carried("State").Children.Should().BeEmpty();
    }

    /// <summary>
    /// The framework's own types are not the domain's shape. Unfolding one would fill the page with
    /// <c>DateTimeOffset</c>'s dozen properties and say nothing about the event.
    /// </summary>
    [Fact]
    public void Reads_nothing_beneath_a_framework_type()
    {
        Carried("OccurredOn").Children.Should().BeEmpty();
    }

    /// <summary>
    /// A type reachable from itself would unfold forever. It is shown once, and the property that
    /// leads back to it is left folded rather than dropped, so the shape is still readable.
    /// </summary>
    [Fact]
    public void Stops_unfolding_a_value_that_holds_its_own_kind()
    {
        var chain = Carried("Chain");

        chain.Children.Select(child => child.Name).Should().Equal("Name", "Next");
        chain.Children.Single(child => child.Name == "Next").Children.Should().BeEmpty();
    }

    /// <summary>
    /// The state tab reads the same properties, so a model holding a value of its own is unfolded
    /// there too rather than only where events are listed.
    /// </summary>
    [Fact]
    public void Unfolds_a_value_a_model_declares_as_well_as_one_an_event_carries()
    {
        Describe(typeof(SampleDcbAggregate)).Properties
            .Single(property => property.Name == "Measurement")
            .Children.Select(child => child.Name).Should().Equal("Height", "Width");
    }

    private static DomainProperty Carried(string name) =>
        DomainTypeDescriber.PropertiesOf(typeof(SampleCarriedEvent))
            .Single(property => property.Name == name);

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
    public void Reads_the_state_of_a_loaded_model()
    {
        var aggregate = new SampleDcbAggregate();

        DomainTypeDescriber.ReadState(aggregate)
            .Should().Contain(property => property.Name == "Name" && property.Value == string.Empty);
    }

    [Fact]
    public void Reads_state_through_the_same_filter_as_the_description()
    {
        DomainTypeDescriber.ReadState(new SampleDcbAggregate())
            .Should().NotContain(property => property.Name == "EventTypeFilter");
    }

    /// <summary>
    /// The value of something holding a shape is the shape, so the cell that would have held one
    /// line of a record's own printout holds nothing and the shape is read underneath it.
    /// </summary>
    [Fact]
    public void Reads_the_values_inside_a_property_holding_a_value_of_its_own()
    {
        var measurement = Held("Measurement");

        measurement.Value.Should().BeNull();
        measurement.Children.Should().BeEquivalentTo(
            new[]
            {
                new DomainPropertyValue("Height", "decimal", "3"),
                new DomainPropertyValue("Width", "decimal", "2")
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Reads_a_list_of_plain_values_as_one_line()
    {
        Held("Notes").Value.Should().Be("first, second");
        Held("Notes").Children.Should().BeEmpty();
    }

    /// <summary>
    /// Numbers as well as strings. A list of them is not a list of objects, so one read through
    /// that interface alone reports the list's own class name and none of what is in it.
    /// </summary>
    [Fact]
    public void Reads_a_list_of_plain_numbers_as_one_line()
    {
        Held("Counts").Value.Should().Be("4, 5");
    }

    /// <summary>
    /// A list of shapes is one row a piece rather than one line for the lot: each element has its
    /// own values, and joining them would run several records into a sentence.
    /// </summary>
    [Fact]
    public void Reads_a_list_of_values_an_element_at_a_time()
    {
        var labels = Held("Labels");

        labels.Value.Should().Be("2 items");
        labels.Children.Select(child => child.Name).Should().Equal("[0]", "[1]");
        labels.Children[1].Children.Single(child => child.Name == "Text").Value.Should().Be("second");
    }

    [Fact]
    public void Reads_the_values_inside_an_element_of_a_list()
    {
        Held("Labels").Children[0].Children
            .Single(child => child.Name == "Size").Children
            .Single(child => child.Name == "Width").Value.Should().Be("6");
    }

    [Fact]
    public void Reads_an_empty_list_as_holding_nothing()
    {
        Held("Nothing").Value.Should().BeNull();
        Held("Nothing").Children.Should().BeEmpty();
    }

    [Fact]
    public void Reads_a_value_that_is_not_there_as_holding_nothing()
    {
        Held("Missing").Value.Should().BeNull();
        Held("Missing").Children.Should().BeEmpty();
    }

    /// <summary>
    /// The same stop the shape has to make, and the value falls back to what the type says about
    /// itself rather than being dropped.
    /// </summary>
    [Fact]
    public void Stops_reading_a_value_that_holds_its_own_kind()
    {
        var next = Held("Chain").Children.Single(child => child.Name == "Next");

        next.Children.Should().BeEmpty();
        next.Value.Should().NotBeNull();
    }

    private static DomainPropertyValue Held(string name) =>
        DomainTypeDescriber.ReadState(new SampleHolding(
                Label: "one",
                Measurement: new SampleMeasurement(2, 3),
                Notes: ["first", "second"],
                Counts: [4, 5],
                Labels:
                [
                    new SampleLabel("first", new SampleMeasurement(6, 7)),
                    new SampleLabel("second", new SampleMeasurement(8, 9))
                ],
                Nothing: [],
                Chain: new SampleChain("head", new SampleChain("tail", null)),
                Missing: null))
            .Single(property => property.Name == name);

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
