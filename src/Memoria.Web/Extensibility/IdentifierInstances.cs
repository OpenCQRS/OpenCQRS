using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Lists the aggregates the store holds, read from the snapshots themselves.
/// </summary>
/// <remarks>
/// A snapshot already carries everything a row shows: the boundary it was written under, which the
/// identifier's values are read back out of, the version it was stored at, and when it was first
/// and last written. So this is one table and no joins.
/// <para>
/// It follows that an aggregate whose events have never been snapshotted is not listed. These are
/// the stored aggregates, and its version is the stored version — a snapshot left at 6 while later
/// events accumulate stays 6 here, and folding its boundary would give more.
/// </para>
/// </remarks>
public static class IdentifierInstances
{
    /// <summary>
    /// Reads one page of the aggregates stored under this shape of identifier.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="shape">Which tags the identifier's values live in.</param>
    /// <param name="modelType">The aggregate's binding key, as <c>name:version</c>.</param>
    /// <param name="tag">Text the boundary must contain, or null to keep them all.</param>
    /// <param name="sort">Which date to order by.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<InstancePage> Page(
        IDcbDbContext context,
        IdentifierShape shape,
        string modelType,
        string? tag,
        InstanceSort sort,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        // Matched on the shape of the boundary, so a narrow identifier lists only what was stored
        // under a narrow boundary. The trailing wildcard would otherwise let "product:%" match a
        // boundary that goes on into a second tag, so anything longer is excluded.
        var pattern = shape.BoundaryPattern;
        var longer = $"{pattern},%";

        var stored = context.DcbSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.SnapshotKind == DcbSnapshotEntity.AggregateKind &&
                               snapshot.ModelType == modelType &&
                               EF.Functions.Like(snapshot.TagQuery, pattern) &&
                               !EF.Functions.Like(snapshot.TagQuery, longer));

        if (!string.IsNullOrWhiteSpace(tag))
        {
            // Lowered on both sides rather than with a provider's case-insensitive operator, so
            // this reads the same against SQL Server as it does against Postgres.
            var wanted = $"%{tag.Trim().ToLower()}%";

            stored = stored.Where(snapshot => EF.Functions.Like(snapshot.TagQuery.ToLower(), wanted));
        }

        var total = await stored.CountAsync(cancellationToken);
        var placed = InstanceQuery.Place(page, total, size);

        var ordered = (sort, descending) switch
        {
            (InstanceSort.Created, true) => stored.OrderByDescending(snapshot => snapshot.CreatedDate),
            (InstanceSort.Created, false) => stored.OrderBy(snapshot => snapshot.CreatedDate),
            (_, true) => stored.OrderByDescending(snapshot => snapshot.UpdatedDate),
            (_, false) => stored.OrderBy(snapshot => snapshot.UpdatedDate)
        };

        var rows = await ordered
            .Skip(placed.Skip)
            .Take(size)
            .Select(snapshot => new
            {
                snapshot.TagQuery, snapshot.Version, snapshot.CreatedDate, snapshot.UpdatedDate
            })
            .ToListAsync(cancellationToken);

        var instances = rows
            .Select(row => new Instance(
                shape.ValuesFromBoundary(row.TagQuery) ?? new Dictionary<string, string>(),
                row.Version,
                row.CreatedDate,
                row.UpdatedDate))
            .ToList();

        return new InstancePage(instances, total, placed.Page, placed.TotalPages);
    }
}

/// <summary>One aggregate the store holds.</summary>
/// <param name="Values">The identifier's values, by the constructor parameter each belongs to.</param>
/// <param name="Version">The version its snapshot was stored at.</param>
/// <param name="Created">When it was first stored.</param>
/// <param name="Updated">When it was last stored.</param>
public sealed record Instance(
    IReadOnlyDictionary<string, string> Values,
    int Version,
    DateTimeOffset Created,
    DateTimeOffset Updated);

/// <summary>One page of a table.</summary>
/// <param name="Rows">The instances on it.</param>
/// <param name="Total">How many the filter left, across every page.</param>
/// <param name="Page">The page these rows are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
public sealed record InstancePage(
    IReadOnlyList<Instance> Rows, int Total, int Page, int TotalPages);
