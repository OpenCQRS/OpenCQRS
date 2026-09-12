using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// What changed between two readings of a model's state, as the compare tab draws it: one table
/// with both values on every row and a mark saying which rows differ. Rows are matched by name at
/// each level, because that is what the state reader keys them by — a property by its name and a
/// list element by its position.
/// </summary>
public class StateDiffTests
{
    private static DomainPropertyValue Line(string name, string? value, string typeName = "string") =>
        new(name, typeName, value);

    private static DomainPropertyValue Shape(
        string name, string? value, params DomainPropertyValue[] children) =>
        new(name, "SampleShape", value, children);

    [Fact]
    public void Marks_nothing_when_both_readings_agree()
    {
        var rows = StateDiff.Of([Line("Name", "a")], [Line("Name", "a")]);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Name", "string", "a", "a", Change.Unchanged, []));
    }

    [Fact]
    public void Marks_a_value_that_differs_as_changed_and_keeps_both_values()
    {
        var rows = StateDiff.Of([Line("Name", "a")], [Line("Name", "b")]);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Name", "string", "a", "b", Change.Changed, []));
    }

    /// <summary>
    /// Null and empty are different values: a property that was unset and is now blank has changed.
    /// </summary>
    [Fact]
    public void Tells_an_unset_value_from_an_empty_one()
    {
        var rows = StateDiff.Of([Line("Name", null)], [Line("Name", "")]);

        rows.Single().Change.Should().Be(Change.Changed);
    }

    [Fact]
    public void Marks_a_property_only_the_later_reading_has_as_added()
    {
        var rows = StateDiff.Of([], [Line("Name", "a")]);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Name", "string", null, "a", Change.Added, []));
    }

    [Fact]
    public void Marks_a_property_only_the_earlier_reading_has_as_removed()
    {
        var rows = StateDiff.Of([Line("Name", "a")], []);

        rows.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Name", "string", "a", null, Change.Removed, []));
    }

    /// <summary>
    /// Matched by name exactly as the reader spells it. Two properties differing only in case are
    /// two properties, which is what the language says of them too.
    /// </summary>
    [Fact]
    public void Matches_names_exactly()
    {
        var rows = StateDiff.Of([Line("name", "a")], [Line("Name", "a")]);

        rows.Select(row => (row.Name, row.Change)).Should().BeEquivalentTo(
            [("name", Change.Removed), ("Name", Change.Added)]);
    }

    [Fact]
    public void Has_nothing_to_say_of_two_empty_readings()
    {
        StateDiff.Of([], []).Should().BeEmpty();
    }

    /// <summary>
    /// A removed row stays where it was among its neighbours rather than being pushed to the end,
    /// so a reader running down the later shape meets it where the earlier one had it.
    /// </summary>
    [Fact]
    public void Keeps_a_removed_row_in_its_place_among_the_rows_around_it()
    {
        var rows = StateDiff.Of(
            [Line("A", "1"), Line("B", "2"), Line("C", "3")],
            [Line("A", "1"), Line("C", "3"), Line("D", "4")]);

        rows.Select(row => (row.Name, row.Change)).Should().Equal(
            ("A", Change.Unchanged),
            ("B", Change.Removed),
            ("C", Change.Unchanged),
            ("D", Change.Added));
    }

    /// <summary>
    /// A change inside a shape is a change to the shape: the parent row is marked as well as the
    /// child, so a reader scanning the top level is told where to look.
    /// </summary>
    [Fact]
    public void Marks_a_shape_changed_when_something_inside_it_changed()
    {
        var rows = StateDiff.Of(
            [Shape("Size", null, Line("Width", "1"), Line("Height", "2"))],
            [Shape("Size", null, Line("Width", "1"), Line("Height", "3"))]);

        var size = rows.Single();

        size.Change.Should().Be(Change.Changed);
        size.Children.Select(child => (child.Name, child.Change)).Should().Equal(
            ("Width", Change.Unchanged),
            ("Height", Change.Changed));
    }

    [Fact]
    public void Leaves_a_shape_unmarked_when_nothing_inside_it_changed()
    {
        var rows = StateDiff.Of(
            [Shape("Size", null, Line("Width", "1"))],
            [Shape("Size", null, Line("Width", "1"))]);

        rows.Single().Change.Should().Be(Change.Unchanged);
    }

    /// <summary>
    /// A list of shapes is read as a count with an element to a row. One more element is a new
    /// position added under the list, and the list itself has changed because its count has.
    /// </summary>
    [Fact]
    public void Marks_an_element_appended_to_a_list_as_added_under_it()
    {
        var rows = StateDiff.Of(
            [Shape("Labels", "1 item", Shape("[0]", null, Line("Text", "a")))],
            [Shape("Labels", "2 items", Shape("[0]", null, Line("Text", "a")), Shape("[1]", null, Line("Text", "b")))]);

        var labels = rows.Single();

        labels.Change.Should().Be(Change.Changed);
        labels.From.Should().Be("1 item");
        labels.To.Should().Be("2 items");
        labels.Children.Select(child => (child.Name, child.Change)).Should().Equal(
            ("[0]", Change.Unchanged),
            ("[1]", Change.Added));
    }

    /// <summary>
    /// A property that was a line and is now a shape has changed, and what it now holds is under
    /// it as added: there was nothing on the earlier side to match those rows against.
    /// </summary>
    [Fact]
    public void Marks_a_value_that_became_a_shape_as_changed_with_its_insides_added()
    {
        var rows = StateDiff.Of(
            [Line("Size", "10x20")],
            [Shape("Size", null, Line("Width", "10"))]);

        var size = rows.Single();

        size.Change.Should().Be(Change.Changed);
        size.From.Should().Be("10x20");
        size.To.Should().BeNull();
        size.Children.Select(child => (child.Name, child.Change)).Should().Equal(("Width", Change.Added));
    }

    /// <summary>
    /// The insides of a row on one side only are carried through marked the same way, so a reader
    /// opening an added shape sees what it holds rather than a bare name.
    /// </summary>
    [Fact]
    public void Carries_the_insides_of_a_removed_shape_as_removed()
    {
        var rows = StateDiff.Of([Shape("Size", null, Line("Width", "1"))], []);

        var size = rows.Single();

        size.Change.Should().Be(Change.Removed);
        size.Children.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new DiffRow("Width", "string", "1", null, Change.Removed, []));
    }

    /// <summary>
    /// The declared type is the later reading's when there is one: it is the shape the model
    /// promises now. A removed row can only say what it was declared as then.
    /// </summary>
    [Fact]
    public void Names_the_type_the_later_reading_declares()
    {
        var rows = StateDiff.Of(
            [Line("Count", "1", typeName: "int"), Line("Gone", "x", typeName: "Guid")],
            [Line("Count", "1", typeName: "long")]);

        rows.Select(row => (row.Name, row.TypeName)).Should().Equal(("Count", "long"), ("Gone", "Guid"));
    }
}
