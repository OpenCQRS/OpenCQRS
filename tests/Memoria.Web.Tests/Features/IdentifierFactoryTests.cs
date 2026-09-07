using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class IdentifierFactoryTests
{
    private sealed record Numbered(string Name, int Count, Guid Reference);

    [Fact]
    public void Reports_the_values_an_identifier_needs()
    {
        IdentifierFactory.Parameters(typeof(SampleDcbAggregateId))
            .Should().ContainSingle().Which.Name.Should().Be("id");
    }

    [Fact]
    public void Reports_the_type_of_each_value()
    {
        IdentifierFactory.Parameters(typeof(Numbered)).Select(parameter => parameter.TypeName)
            .Should().Equal("string", "int", "Guid");
    }

    [Fact]
    public void Builds_an_identifier_from_the_values_given()
    {
        var created = IdentifierFactory.Create(typeof(SampleDcbAggregateId),
            new Dictionary<string, string?> { ["id"] = "abc-1" });

        created.Error.Should().BeNull();
        created.Instance.Should().BeOfType<SampleDcbAggregateId>()
            .Which.Id.Should().Be("abc-1");
    }

    [Fact]
    public void Converts_each_value_to_the_type_the_constructor_wants()
    {
        var reference = Guid.NewGuid();

        var created = IdentifierFactory.Create(typeof(Numbered), new Dictionary<string, string?>
        {
            ["name"] = "widget", ["count"] = "42", ["reference"] = reference.ToString()
        });

        created.Instance.Should().Be(new Numbered("widget", 42, reference));
    }

    /// <summary>
    /// The names come off a query string, where nobody should have to match the constructor's
    /// casing.
    /// </summary>
    [Fact]
    public void Matches_value_names_whatever_their_casing()
    {
        var reference = Guid.NewGuid();

        var created = IdentifierFactory.Create(typeof(Numbered), new Dictionary<string, string?>
        {
            ["NAME"] = "widget", ["count"] = "42", ["Reference"] = reference.ToString()
        });

        created.Instance.Should().Be(new Numbered("widget", 42, reference));
    }

    [Fact]
    public void Refuses_to_build_when_a_value_is_missing()
    {
        var created = IdentifierFactory.Create(typeof(Numbered),
            new Dictionary<string, string?> { ["name"] = "widget" });

        created.Instance.Should().BeNull();
        created.Error.Should().Contain("Count");
    }

    [Fact]
    public void Reports_a_value_that_is_not_of_the_type_wanted()
    {
        var created = IdentifierFactory.Create(typeof(Numbered), new Dictionary<string, string?>
        {
            ["name"] = "widget", ["count"] = "not a number", ["reference"] = Guid.NewGuid().ToString()
        });

        created.Instance.Should().BeNull();
        created.Error.Should().Contain("Count");
    }

    [Fact]
    public void Reports_a_type_it_cannot_build_at_all()
    {
        var created = IdentifierFactory.Create(typeof(string), new Dictionary<string, string?>());

        created.Instance.Should().BeNull();
        created.Error.Should().NotBeNull();
    }

    [Fact]
    public void Knows_when_every_value_has_been_supplied()
    {
        IdentifierFactory.IsComplete(typeof(SampleDcbAggregateId),
            new Dictionary<string, string?> { ["id"] = "abc" }).Should().BeTrue();

        IdentifierFactory.IsComplete(typeof(SampleDcbAggregateId),
            new Dictionary<string, string?> { ["id"] = "" }).Should().BeFalse();

        IdentifierFactory.IsComplete(typeof(SampleDcbAggregateId),
            new Dictionary<string, string?>()).Should().BeFalse();
    }
}
