using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The paging arithmetic the pager is drawn from and the query is skipped by. Narrowing, ordering
/// and paging themselves happen in the database, and are covered against a real store rather than
/// here.
/// </summary>
public class InstanceQueryTests
{
    [Fact]
    public void Skips_nothing_to_reach_the_first_page()
    {
        var placed = InstanceQuery.Place(page: 1, total: 30, size: 10);

        placed.Page.Should().Be(1);
        placed.Skip.Should().Be(0);
        placed.TotalPages.Should().Be(3);
    }

    [Fact]
    public void Skips_the_pages_before_the_one_asked_for()
    {
        InstanceQuery.Place(page: 3, total: 30, size: 10).Skip.Should().Be(20);
    }

    [Fact]
    public void Counts_a_part_full_last_page()
    {
        InstanceQuery.Place(page: 1, total: 23, size: 10).TotalPages.Should().Be(3);
    }

    [Fact]
    public void Brings_a_page_before_the_first_one_back_to_the_first()
    {
        var placed = InstanceQuery.Place(page: 0, total: 30, size: 10);

        placed.Page.Should().Be(1);
        placed.Skip.Should().Be(0);
    }

    /// <summary>
    /// A page number left over from a wider page size, or from before a filter was applied, would
    /// otherwise skip past every row and show an empty table under a pager promising rows.
    /// </summary>
    [Fact]
    public void Brings_a_page_past_the_last_one_back_to_the_last()
    {
        var placed = InstanceQuery.Place(page: 99, total: 23, size: 10);

        placed.Page.Should().Be(3);
        placed.Skip.Should().Be(20);
    }

    [Fact]
    public void Reports_one_page_when_there_is_nothing_to_show()
    {
        var placed = InstanceQuery.Place(page: 1, total: 0, size: 10);

        placed.Page.Should().Be(1);
        placed.TotalPages.Should().Be(1);
        placed.Skip.Should().Be(0);
    }

    [Theory]
    [InlineData("created", InstanceSort.Created)]
    [InlineData("updated", InstanceSort.Updated)]
    [InlineData("CREATED", InstanceSort.Created)]
    public void Reads_the_column_to_order_by(string asked, InstanceSort expected)
    {
        InstanceQuery.SortOf(asked).Should().Be(expected);
    }

    /// <summary>
    /// Newest activity first is what a list of aggregates is usually being read for.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void Orders_by_the_updated_date_when_nothing_sensible_was_asked_for(string? asked)
    {
        InstanceQuery.SortOf(asked).Should().Be(InstanceSort.Updated);
    }

    /// <summary>
    /// The size arrives from the address bar, where anything at all could be typed.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void Accepts_an_offered_page_size(int size)
    {
        InstanceQuery.PageSizeOf(size.ToString()).Should().Be(size);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("1000")]
    [InlineData("not a number")]
    [InlineData(null)]
    public void Falls_back_to_ten_for_a_size_that_is_not_offered(string? size)
    {
        InstanceQuery.PageSizeOf(size).Should().Be(10);
    }
}
