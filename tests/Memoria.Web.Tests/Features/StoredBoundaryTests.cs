using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Reading a boundary back out of the store the way a page shows it: the tags it selects on, and
/// how they combine said in words rather than spelled in a separator.
/// </summary>
/// <remarks>
/// The store writes a boundary in one string because it has to be one column. What that string
/// joins with a comma and what it joins with an ampersand are two different boundaries, and the
/// difference is a punctuation mark — which is exactly what a reader cannot be expected to notice.
/// </remarks>
public class StoredBoundaryTests
{
    [Fact]
    public void Reads_a_boundary_of_one_tag()
    {
        var boundary = StoredBoundary.Read("product:p-2cfa");

        boundary.Tags.Should().Equal("product:p-2cfa");
        boundary.Text.Should().Be("product:p-2cfa");
    }

    /// <summary>
    /// One tag is a union and an intersection at once, so neither word tells it from the other and
    /// naming one would be a word to read past on every such row.
    /// </summary>
    [Fact]
    public void Names_no_combination_over_a_single_tag()
    {
        StoredBoundary.Read("product:p-2cfa").Combination.Should().BeNull();
    }

    /// <summary>
    /// A union: the store writes each tag as a group of its own, and groups are joined by a comma.
    /// </summary>
    [Fact]
    public void Reads_a_union_of_tags()
    {
        var boundary = StoredBoundary.Read("product:p-2cfa,sku:CK-c205");

        boundary.Tags.Should().Equal("product:p-2cfa", "sku:CK-c205");
        boundary.Text.Should().Be("product:p-2cfa, sku:CK-c205");
        boundary.Combination.Should().Be("(AnyOf)");
    }

    /// <summary>
    /// An intersection: the store writes the tags into one group, joined by an ampersand. The tags
    /// are listed the same way either way — the words beside them are what tells the two apart.
    /// </summary>
    [Fact]
    public void Reads_an_intersection_of_tags()
    {
        var boundary = StoredBoundary.Read("order:o-c4da&product:p-1ec3");

        boundary.Tags.Should().Equal("order:o-c4da", "product:p-1ec3");
        boundary.Text.Should().Be("order:o-c4da, product:p-1ec3");
        boundary.Combination.Should().Be("(AllOf)");
    }

    /// <summary>
    /// A separator a tag carries itself is escaped when the boundary is written, so it is part of
    /// the tag rather than a place the boundary comes apart — and it is shown as the tag has it,
    /// without the backslash that got it past the separator.
    /// </summary>
    [Fact]
    public void Keeps_a_separator_a_tag_carries_of_its_own()
    {
        var boundary = StoredBoundary.Read(@"note:a\,b,note:c");

        boundary.Tags.Should().Equal("note:a,b", "note:c");
        boundary.Combination.Should().Be("(AnyOf)");
    }

    [Fact]
    public void Keeps_an_escaped_ampersand_inside_a_tag()
    {
        StoredBoundary.Read(@"note:a\&b").Tags.Should().Equal("note:a&b");
    }

    /// <summary>
    /// The escape character itself, escaped, is one backslash in the tag rather than an escape of
    /// whatever came after it.
    /// </summary>
    [Fact]
    public void Keeps_an_escaped_escape_character()
    {
        StoredBoundary.Read(@"note:a\\b").Tags.Should().Equal(@"note:a\b");
    }

    /// <summary>
    /// Neither constructor produces a boundary mixing the two separators, so one that arrives is a
    /// shape this cannot name. The tags are still listed; the word beside them is not guessed at.
    /// </summary>
    [Fact]
    public void Names_no_combination_for_a_boundary_it_cannot_read_as_either()
    {
        var boundary = StoredBoundary.Read("a:1&b:2,c:3");

        boundary.Tags.Should().Equal("a:1", "b:2", "c:3");
        boundary.Combination.Should().BeNull();
    }
}
