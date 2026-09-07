namespace Memoria.Web.Extensibility;

/// <summary>
/// The rows-per-page choices, and working out which rows a page number asks for.
/// </summary>
/// <remarks>
/// Narrowing, ordering and paging themselves are the database's work — see
/// <see cref="IdentifierInstances.Page"/>. What is left here is the arithmetic around them, which
/// has to agree with the pager drawn on the page and is worth pinning on its own.
/// </remarks>
public static class InstanceQuery
{
    /// <summary>The rows-per-page sizes on offer.</summary>
    /// <remarks>
    /// Also the allowlist. The size arrives from the address bar, and anything outside this set
    /// would let a caller ask for the whole table at once.
    /// </remarks>
    public static readonly int[] PageSizes = [10, 25, 50, 100];

    /// <summary>The size used when none was asked for, or one that is not on offer was.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Reads a rows-per-page size, falling back to the default for anything not on offer.
    /// </summary>
    /// <param name="size">The size as it arrived, or null.</param>
    public static int PageSizeOf(string? size) =>
        int.TryParse(size, out var parsed) && PageSizes.Contains(parsed) ? parsed : DefaultPageSize;

    /// <summary>
    /// Reads the column to order by, falling back to the updated date.
    /// </summary>
    /// <param name="sort">The column as it arrived, or null.</param>
    public static InstanceSort SortOf(string? sort) =>
        Enum.TryParse<InstanceSort>(sort, ignoreCase: true, out var parsed) ? parsed : InstanceSort.Updated;

    /// <summary>
    /// Works out which page is really being asked for, and how many rows to skip to reach it.
    /// </summary>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="total">How many rows there are in all.</param>
    /// <param name="size">The rows per page.</param>
    /// <remarks>
    /// The page number is brought back inside the table rather than trusted. One left over from a
    /// wider page size, or from before a filter was applied, would otherwise skip past everything
    /// and show an empty table under a pager saying there are rows.
    /// </remarks>
    public static PlacedPage Place(int page, int total, int size)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var current = Math.Clamp(page, 1, totalPages);

        return new PlacedPage(current, totalPages, (current - 1) * size);
    }
}

/// <summary>Where one page sits in the whole table.</summary>
/// <param name="Page">The page being shown, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Skip">How many rows come before this page.</param>
public sealed record PlacedPage(int Page, int TotalPages, int Skip);

/// <summary>Which date a list of aggregates is ordered by.</summary>
public enum InstanceSort
{
    /// <summary>When the earliest event in the boundary was written.</summary>
    Created,

    /// <summary>When the latest was.</summary>
    Updated
}
