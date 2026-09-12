namespace Memoria.Web.Extensibility;

/// <summary>
/// The strip of page numbers a pager draws, and where its two &ldquo;&hellip;&rdquo; links go.
/// </summary>
/// <param name="Pages">The numbers to draw, in order, never more than <see cref="Most"/> of them.</param>
/// <param name="Back">The page the leading &ldquo;&hellip;&rdquo; reaches, or null when the strip starts at the first.</param>
/// <param name="Forward">The page the trailing &ldquo;&hellip;&rdquo; reaches, or null when the strip ends at the last.</param>
/// <remarks>
/// A table grows with the log behind it, so the strip under it cannot: a dropdown of every page was
/// one option per page, and a log of ten thousand pages drew ten thousand of them. What is drawn
/// instead is one group of five at a time.
/// <para>
/// Groups rather than a window sliding around the page being read: stepping from one page to the
/// next inside a group leaves the numbers where they were, so a reader crossing a table moves along
/// a strip that stands still. The groups meet rather than overlap — back from the second lands on
/// the last page of the first, forward from the first on the first page of the second — so the two
/// ellipses walk a long table group by group without repeating a number.
/// </para>
/// </remarks>
public sealed record PageWindow(IReadOnlyList<int> Pages, int? Back, int? Forward)
{
    /// <summary>How many numbers are drawn at once, and so how long a group is.</summary>
    public const int Most = 5;

    /// <summary>
    /// Works out the group the page being read falls in.
    /// </summary>
    /// <param name="page">The page being read, from one.</param>
    /// <param name="totalPages">How many pages there are, never fewer than one.</param>
    /// <remarks>
    /// The page is brought back inside the table first. It is clamped before it reaches the table
    /// as well — see <see cref="InstanceQuery.Place"/> — but a strip drawn from a number the
    /// address bar carried and nothing sat under would be empty rather than merely wrong.
    /// </remarks>
    public static PageWindow Of(int page, int totalPages)
    {
        var last = Math.Max(1, totalPages);
        var current = Math.Clamp(page, 1, last);

        var opens = (current - 1) / Most * Most + 1;
        var closes = Math.Min(opens + Most - 1, last);

        return new PageWindow(
            [..Enumerable.Range(opens, closes - opens + 1)],
            Back: opens > 1 ? opens - 1 : null,
            Forward: closes < last ? closes + 1 : null);
    }
}
