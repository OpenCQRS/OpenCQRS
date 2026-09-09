using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which page numbers the pager draws, and where the two &ldquo;&hellip;&rdquo; links go. A table
/// grows without bound, so the strip of numbers under it cannot: it shows one group of ten at a
/// time and the ellipses step between the groups.
/// </summary>
public class PageWindowTests
{
    [Fact]
    public void Lists_every_page_of_a_short_table()
    {
        PageWindow.Of(page: 2, totalPages: 3).Pages.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Ten is the whole strip, so there is no group before or after it to reach.
    /// </summary>
    [Fact]
    public void Offers_no_way_out_of_a_table_that_fits_in_one_group()
    {
        var window = PageWindow.Of(page: 2, totalPages: 10);

        window.Pages.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        window.Back.Should().BeNull();
        window.Forward.Should().BeNull();
    }

    /// <summary>
    /// The number of links is what is capped, not the table: a log of ten thousand pages draws the
    /// same ten as a log of eleven.
    /// </summary>
    [Fact]
    public void Never_draws_more_than_ten_numbers()
    {
        PageWindow.Of(page: 1, totalPages: 10_000).Pages.Should().HaveCount(10);
    }

    [Fact]
    public void Opens_on_the_first_group_with_a_way_forward()
    {
        var window = PageWindow.Of(page: 1, totalPages: 25);

        window.Pages.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        window.Back.Should().BeNull();
        window.Forward.Should().Be(11);
    }

    /// <summary>
    /// Stepping through a group leaves the numbers where they are, so the one being read moves
    /// along a strip that stands still rather than the strip sliding under it.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(10)]
    public void Keeps_the_same_group_while_the_page_is_inside_it(int page)
    {
        PageWindow.Of(page, totalPages: 25).Pages.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Fact]
    public void Moves_on_to_the_next_group_past_the_tenth_page()
    {
        var window = PageWindow.Of(page: 11, totalPages: 25);

        window.Pages.Should().Equal(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        window.Back.Should().Be(10);
        window.Forward.Should().Be(21);
    }

    /// <summary>
    /// The groups meet rather than overlap: back from the second lands on the last page of the
    /// first, forward from the first on the first page of the second.
    /// </summary>
    [Fact]
    public void Ends_the_last_group_at_the_last_page()
    {
        var window = PageWindow.Of(page: 24, totalPages: 25);

        window.Pages.Should().Equal(21, 22, 23, 24, 25);
        window.Back.Should().Be(20);
        window.Forward.Should().BeNull();
    }

    /// <summary>
    /// The page arrives from the address bar. It is clamped before it reaches the table, but a
    /// strip drawn from an unclamped number would be empty rather than wrong-looking, so it is
    /// brought inside here too.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void Draws_the_first_group_for_a_page_before_the_first(int page)
    {
        PageWindow.Of(page, totalPages: 25).Pages.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Fact]
    public void Draws_the_last_group_for_a_page_past_the_last()
    {
        PageWindow.Of(page: 99, totalPages: 25).Pages.Should().Equal(21, 22, 23, 24, 25);
    }

    /// <summary>
    /// An empty table is one page of nothing, the way the arithmetic that counts them reports it.
    /// </summary>
    [Fact]
    public void Draws_the_one_page_an_empty_table_still_has()
    {
        var window = PageWindow.Of(page: 1, totalPages: 1);

        window.Pages.Should().Equal(1);
        window.Back.Should().BeNull();
        window.Forward.Should().BeNull();
    }
}
