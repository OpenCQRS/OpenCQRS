using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The Json tab shows a stored payload as the store wrote it, laid out to be read rather than as
/// the one line the store holds, with each kind of token marked so the stylesheet can colour it.
/// What is pinned here is the shape of that markup: the layout, the classes, and that nothing in a
/// payload can reach the page as markup of its own.
/// </summary>
public class JsonViewTests
{
    [Fact]
    public void Lays_an_object_out_one_property_per_line_and_indented()
    {
        var rendered = JsonView.Render("""{"Name":"Kettle","Lines":[1,2],"Owner":{"Id":7}}""");

        rendered.Error.Should().BeNull();
        rendered.Markup.Should().Be(
            "{\n" +
            "  <span class=\"json-key\">\"Name\"</span>: <span class=\"json-string\">\"Kettle\"</span>,\n" +
            "  <span class=\"json-key\">\"Lines\"</span>: [\n" +
            "    <span class=\"json-number\">1</span>,\n" +
            "    <span class=\"json-number\">2</span>\n" +
            "  ],\n" +
            "  <span class=\"json-key\">\"Owner\"</span>: {\n" +
            "    <span class=\"json-key\">\"Id\"</span>: <span class=\"json-number\">7</span>\n" +
            "  }\n" +
            "}");
    }

    [Fact]
    public void Marks_each_kind_of_value_with_its_own_class()
    {
        var rendered = JsonView.Render("""{"a":true,"b":false,"c":null,"d":1.5,"e":"x"}""");

        rendered.Markup.Should().Contain("<span class=\"json-bool\">true</span>")
            .And.Contain("<span class=\"json-bool\">false</span>")
            .And.Contain("<span class=\"json-null\">null</span>")
            .And.Contain("<span class=\"json-number\">1.5</span>")
            .And.Contain("<span class=\"json-string\">\"x\"</span>");
    }

    [Fact]
    public void Keeps_an_empty_object_and_array_on_one_line()
    {
        var rendered = JsonView.Render("""{"a":{},"b":[]}""");

        rendered.Markup.Should().Be(
            "{\n" +
            "  <span class=\"json-key\">\"a\"</span>: {},\n" +
            "  <span class=\"json-key\">\"b\"</span>: []\n" +
            "}");
    }

    /// <summary>
    /// A payload is data the application stored, not markup the page wrote, so anything in it that
    /// looks like a tag has to reach the page as text.
    /// </summary>
    [Fact]
    public void Escapes_markup_inside_keys_and_values()
    {
        var rendered = JsonView.Render("""{"<b>":"<script>alert(1)</script> & co"}""");

        rendered.Markup.Should().NotContain("<script>")
            .And.NotContain("<b>")
            .And.Contain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; co")
            .And.Contain("<span class=\"json-key\">\"&lt;b&gt;\"</span>");
    }

    [Fact]
    public void Shows_a_string_as_the_store_escaped_it()
    {
        var rendered = JsonView.Render("""{"a":"line\nbreak \"quoted\""}""");

        rendered.Markup.Should().Contain("<span class=\"json-string\">\"line\\nbreak \\\"quoted\\\"\"</span>");
    }

    [Fact]
    public void Renders_a_payload_that_is_not_an_object()
    {
        JsonView.Render("[1,\"two\"]").Markup.Should().Be(
            "[\n" +
            "  <span class=\"json-number\">1</span>,\n" +
            "  <span class=\"json-string\">\"two\"</span>\n" +
            "]");
    }

    [Fact]
    public void Says_why_a_payload_that_is_not_json_cannot_be_shown()
    {
        var rendered = JsonView.Render("{not json");

        rendered.Markup.Should().BeNull();
        rendered.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Says_an_empty_payload_is_empty(string data)
    {
        var rendered = JsonView.Render(data);

        rendered.Markup.Should().BeNull();
        rendered.Error.Should().Be("The stored payload is empty.");
    }
}
