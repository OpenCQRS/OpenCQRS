using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Lists the models the store holds, read from the snapshots themselves.
/// </summary>
/// <remarks>
/// A snapshot already carries everything a row shows: the boundary it was written under, which the
/// identifier's values are read back out of, the version it was stored at, and when it was first
/// and last written. So this is one table and no joins.
/// <para>
/// It follows that a model whose events have never been snapshotted is not listed. These are the
/// stored aggregates and projections, and the version is the stored version — a snapshot left at 6
/// while later events accumulate stays 6 here, and folding its boundary would give more.
/// </para>
/// </remarks>
public static class IdentifierInstances
{
    /// <summary>
    /// Reads one page of the stored models.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="kind">Whether the rows wanted are aggregates or projections.</param>
    /// <param name="modelType">The model's binding key, as <c>name:version</c>, or null for every
    /// model of the kind.</param>
    /// <param name="shape">Which tags the identifier's values live in, or null for every boundary
    /// whatever its shape.</param>
    /// <param name="text">Text the row's boundary must carry, or null to keep them all.</param>
    /// <param name="sort">Which date to order by.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// The kind is the one narrowing always applied. One table holds both models, and an aggregate
    /// and a projection may legitimately be bound to the same name and version, so a list that asked
    /// by name alone could show one under the other's page — and a list asking by nothing else at
    /// all would show both kinds on either page.
    /// <para>
    /// The other two narrow or not, which is what lets one query serve a page listing everything of
    /// a kind, everything of one model, and one identifier's own. A row carries its key and its
    /// boundary either way, so a page listing several models can still name each row and lead from
    /// it; the values are only unfolded when there is a shape to unfold them with.
    /// </para>
    /// </remarks>
    public static async Task<InstancePage> Page(
        IDcbDbContext context,
        DcbModelKind kind,
        string? modelType,
        IdentifierShape? shape,
        string? text,
        InstanceSort sort,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        var snapshotKind = kind.SnapshotKind();

        var stored = context.DcbSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.SnapshotKind == snapshotKind);

        if (modelType is not null)
        {
            stored = stored.Where(snapshot => snapshot.ModelType == modelType);
        }

        if (shape is not null)
        {
            // Matched on the shape of the boundary, so a narrow identifier lists only what was
            // stored under a narrow boundary. The trailing wildcard would otherwise let "product:%"
            // match a boundary that goes on into a second tag, so anything longer is excluded.
            var pattern = shape.BoundaryPattern;
            var longer = $"{pattern},%";

            stored = stored.Where(snapshot => EF.Functions.Like(snapshot.TagQuery, pattern) &&
                                              !EF.Functions.Like(snapshot.TagQuery, longer));
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Lowered on both sides rather than with a provider's case-insensitive operator, so
            // this reads the same against SQL Server as it does against Postgres.
            var wanted = $"%{text.Trim().ToLower()}%";

            // The boundary it was folded from, and only that. The store id is the store's own key
            // rather than anything a reader chose — the model's id and the version of its binding
            // spliced together — so matching it too answered a search for one tag value with rows
            // whose key merely spelled it, and nothing on the row told the two apart.
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
                snapshot.ModelType,
                snapshot.TagQuery,
                snapshot.Version,
                snapshot.CreatedDate,
                snapshot.UpdatedDate
            })
            .ToListAsync(cancellationToken);

        var instances = rows
            .Select(row => new Instance(
                shape?.ValuesFromBoundary(row.TagQuery) ?? new Dictionary<string, string>(),
                row.ModelType,
                row.TagQuery,
                row.Version,
                row.CreatedDate,
                row.UpdatedDate))
            .ToList();

        return new InstancePage(instances, total, placed.Page, placed.TotalPages);
    }
}

/// <summary>One model the store holds.</summary>
/// <param name="Values">The identifier's values, by the constructor parameter each belongs to.
/// Empty when the list was not narrowed to one identifier, because there is then no one shape to
/// unfold a boundary with.</param>
/// <param name="ModelType">The binding key it was stored under, as <c>name:version</c>.</param>
/// <param name="Boundary">The boundary it was folded under, in canonical form.</param>
/// <param name="Version">The version its snapshot was stored at.</param>
/// <param name="Created">When it was first stored.</param>
/// <param name="Updated">When it was last stored.</param>
/// <remarks>
/// The binding key and the boundary are carried whether or not they are shown. They are what a list
/// of several models names a row by and leads from — see <see cref="StoredModels"/> — and what a
/// list of one identifier's rows has already used up in narrowing to them.
/// </remarks>
public sealed record Instance(
    IReadOnlyDictionary<string, string> Values,
    string ModelType,
    string Boundary,
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
