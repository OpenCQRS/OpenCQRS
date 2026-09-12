using FluentAssertions;
using Memoria.Web.Components.Shared;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// A filtered table marks the text a row was kept for, wherever the store looked for it. What is
/// pinned here is that the marks fall exactly where the store's match did: the same text, matched
/// the same way, and nothing marked when nothing was typed.
/// </summary>
public class HighlightTests
{
    [Fact]
    public void Marks_each_place_the_text_occurs()
    {
        var runs = Highlight.Runs("order-7 for order-8", "order");

        runs.Should().Equal(
            new TextRun("order", IsMatch: true),
            new TextRun("-7 for ", IsMatch: false),
            new TextRun("order", IsMatch: true),
            new TextRun("-8", IsMatch: false));
    }

    [Fact]
    public void Matches_regardless_of_case_and_keeps_the_text_as_written()
    {
        var runs = Highlight.Runs("Kettle", "KET");

        runs.Should().Equal(
            new TextRun("Ket", IsMatch: true),
            new TextRun("tle", IsMatch: false));
    }

    [Fact]
    public void Ignores_the_space_around_what_was_typed_as_the_store_does()
    {
        var runs = Highlight.Runs("abc", "  b ");

        runs.Should().Equal(
            new TextRun("a", IsMatch: false),
            new TextRun("b", IsMatch: true),
            new TextRun("c", IsMatch: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Marks_nothing_when_nothing_was_typed(string? needle)
    {
        var runs = Highlight.Runs("abc", needle);

        runs.Should().Equal(new TextRun("abc", IsMatch: false));
    }

    [Fact]
    public void Marks_nothing_when_the_text_is_not_there()
    {
        var runs = Highlight.Runs("abc", "z");

        runs.Should().Equal(new TextRun("abc", IsMatch: false));
    }

    [Fact]
    public void Marks_the_whole_value_when_all_of_it_matches()
    {
        var runs = Highlight.Runs("abc", "ABC");

        runs.Should().Equal(new TextRun("abc", IsMatch: true));
    }

    [Fact]
    public void Does_not_overlap_the_marks()
    {
        var runs = Highlight.Runs("aaa", "aa");

        runs.Should().Equal(
            new TextRun("aa", IsMatch: true),
            new TextRun("a", IsMatch: false));
    }

    [Fact]
    public void Leaves_an_empty_value_empty()
    {
        var runs = Highlight.Runs("", "a");

        runs.Should().BeEmpty();
    }
}
