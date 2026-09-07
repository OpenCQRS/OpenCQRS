using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Finds the instances of one identifier that the store already holds.
/// </summary>
/// <remarks>
/// Every part of this — narrowing to the identifier's tags, pairing the tags that were written
/// together, the created date, the filter, the order and the page — is done by the database. The
/// page reads one page's worth of rows however large the store is, so there is no cap on what it
/// will pull back and no truncation to be wrong about.
/// </remarks>
public static class IdentifierInstances
{
    /// <summary>
    /// The most tags one identifier's boundary may be built from.
    /// </summary>
    /// <remarks>
    /// Each one is another join onto the tag table, and the join has to be written out rather than
    /// looped, so there is a limit. Boundaries in practice use one or two.
    /// </remarks>
    public const int MaxSlots = 4;

    /// <summary>
    /// Stands in for a tag column this identifier does not use.
    /// </summary>
    /// <remarks>
    /// Every column is assigned in every branch, empty where there is no tag. A column left unset
    /// cannot be grouped on: the database is given a member that was never projected.
    /// </remarks>
    private const string Absent = "";

    /// <summary>
    /// Reads one page of the instances of this shape.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="shape">Which tags the identifier's values live in.</param>
    /// <param name="eventTypes">
    /// The binding keys of the events the aggregate applies, or empty to count them all.
    /// </param>
    /// <param name="tag">Text a tag must contain, or null to keep them all.</param>
    /// <param name="sort">Which date to order by.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Read from the event tags rather than the snapshots, so an aggregate that has events but was
    /// never snapshotted still appears. A row is one set of tag values that were written together
    /// at some position, which is why a two-tag identifier lists only the combinations that really
    /// occur rather than every pairing. An instance written at several positions is one row: created
    /// when the earliest was written, updated when the latest was, and versioned by how many there
    /// are.
    /// </remarks>
    public static async Task<InstancePage> Page(
        IDcbDbContext context,
        IdentifierShape shape,
        IReadOnlyList<string> eventTypes,
        string? tag,
        InstanceSort sort,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        var combinations = Combinations(context, shape, eventTypes);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var wanted = tag.Trim().ToLower();

            // Any tag at that position, not only the ones the identifier is built from: a product
            // found by its id is still the product with that sku, and the filter finds it there.
            // Lowered on both sides rather than with a provider's case-insensitive operator, so
            // this reads the same against SQL Server as it does against Postgres.
            combinations = combinations.Where(combination => context.DcbEventTags
                .Any(candidate => candidate.Position == combination.Position &&
                                  candidate.Tag.ToLower().Contains(wanted)));
        }

        var grouped = combinations
            .GroupBy(combination => new
            {
                combination.Tag1, combination.Tag2, combination.Tag3, combination.Tag4
            })
            .Select(instance => new
            {
                instance.Key,
                Created = instance.Min(combination => combination.Created),
                Updated = instance.Max(combination => combination.Created),

                // The fold's version without folding: one per event the aggregate applies, which
                // is what Version counts as those events go through it.
                Version = instance.Count()
            });

        var total = await grouped.CountAsync(cancellationToken);
        var placed = InstanceQuery.Place(page, total, size);

        var ordered = (sort, descending) switch
        {
            (InstanceSort.Created, true) => grouped.OrderByDescending(instance => instance.Created),
            (InstanceSort.Created, false) => grouped.OrderBy(instance => instance.Created),
            (_, true) => grouped.OrderByDescending(instance => instance.Updated),
            (_, false) => grouped.OrderBy(instance => instance.Updated)
        };

        var rows = await ordered
            .Skip(placed.Skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        var instances = rows
            .Select(row => new Instance(
                shape.ValuesFrom(Tags(row.Key.Tag1, row.Key.Tag2, row.Key.Tag3, row.Key.Tag4))
                ?? new Dictionary<string, string>(),
                row.Created,
                row.Updated,
                row.Version))
            .ToList();

        return new InstancePage(instances, total, placed.Page, placed.TotalPages);
    }

    /// <summary>
    /// The tag values written together at one position, one column per tag the shape needs.
    /// </summary>
    /// <remarks>
    /// The first tag selects the positions; each further tag is joined onto the same position,
    /// which is what "written together" means and what a prefix match on its own cannot say.
    /// </remarks>
    private static IQueryable<Combination> Combinations(
        IDcbDbContext context, IdentifierShape shape, IReadOnlyList<string> eventTypes)
    {
        if (shape.Slots.Count > MaxSlots)
        {
            throw new InvalidOperationException(
                $"{shape.Identifier.Name} is built from {shape.Slots.Count} tags; at most {MaxSlots} are supported.");
        }

        var prefixes = shape.TagKeys.Select(key => $"{key}:").ToList();

        var combinations = At(context, prefixes[0], eventTypes)
            .Select(tag => new Combination
            {
                Position = tag.Position,
                Created = tag.Created,
                Tag1 = tag.Tag,
                Tag2 = Absent,
                Tag3 = Absent,
                Tag4 = Absent
            });

        if (prefixes.Count > 1)
        {
            combinations = combinations.Join(At(context, prefixes[1], eventTypes),
                combination => combination.Position, tag => tag.Position,
                (combination, tag) => new Combination
                {
                    Position = combination.Position,
                    Created = combination.Created,
                    Tag1 = combination.Tag1,
                    Tag2 = tag.Tag,
                    Tag3 = Absent,
                    Tag4 = Absent
                });
        }

        if (prefixes.Count > 2)
        {
            combinations = combinations.Join(At(context, prefixes[2], eventTypes),
                combination => combination.Position, tag => tag.Position,
                (combination, tag) => new Combination
                {
                    Position = combination.Position,
                    Created = combination.Created,
                    Tag1 = combination.Tag1,
                    Tag2 = combination.Tag2,
                    Tag3 = tag.Tag,
                    Tag4 = Absent
                });
        }

        if (prefixes.Count > 3)
        {
            combinations = combinations.Join(At(context, prefixes[3], eventTypes),
                combination => combination.Position, tag => tag.Position,
                (combination, tag) => new Combination
                {
                    Position = combination.Position,
                    Created = combination.Created,
                    Tag1 = combination.Tag1,
                    Tag2 = combination.Tag2,
                    Tag3 = combination.Tag3,
                    Tag4 = tag.Tag
                });
        }

        return combinations;
    }

    /// <summary>
    /// The tags starting with one prefix, each carrying the date its event was written.
    /// </summary>
    /// <remarks>
    /// Joined here rather than reached through the navigation property, so the date is a column of
    /// this subquery. Left to the navigation, the grouping below is translated with a correlated
    /// subquery re-run for every group instead of a plain aggregate.
    /// </remarks>
    private static IQueryable<TaggedEvent> At(
        IDcbDbContext context, string prefix, IReadOnlyList<string> eventTypes)
    {
        var tagged = context.DcbEventTags
            .AsNoTracking()
            .Where(tag => tag.Tag.StartsWith(prefix))
            .Join(context.DcbEvents, tag => tag.Position, written => written.Position,
                (tag, written) => new TaggedEvent
                {
                    Position = tag.Position,
                    Tag = tag.Tag,
                    Created = written.CreatedDate,
                    EventType = written.EventType
                });

        // An event the aggregate does not apply leaves its version and its dates alone, so it is
        // not one of these rows. No filter at all means it applies everything in its boundary.
        return eventTypes.Count == 0
            ? tagged
            : tagged.Where(tag => eventTypes.Contains(tag.EventType));
    }

    /// <summary>One tag, with when its event was written and what kind it was.</summary>
    private sealed class TaggedEvent
    {
        public long Position { get; init; }

        public string Tag { get; init; } = null!;

        public DateTimeOffset Created { get; init; }

        public string EventType { get; init; } = null!;
    }

    private static IEnumerable<string> Tags(params string[] tags) =>
        tags.Where(tag => tag.Length > 0);

    /// <summary>One position's tags, one column per tag the identifier is built from.</summary>
    private sealed class Combination
    {
        public long Position { get; init; }

        public DateTimeOffset Created { get; init; }

        public string Tag1 { get; init; } = null!;

        public string Tag2 { get; init; } = null!;

        public string Tag3 { get; init; } = null!;

        public string Tag4 { get; init; } = null!;
    }
}

/// <summary>One aggregate the store holds, before it is loaded.</summary>
/// <param name="Values">The identifier's values, by the constructor parameter each belongs to.</param>
/// <param name="Created">When the earliest event carrying these tags was written.</param>
/// <param name="Updated">When the latest was.</param>
/// <param name="Version">How many of them the aggregate applies.</param>
public sealed record Instance(
    IReadOnlyDictionary<string, string> Values,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    int Version);

/// <summary>One page of a table.</summary>
/// <param name="Rows">The instances on it.</param>
/// <param name="Total">How many the filter left, across every page.</param>
/// <param name="Page">The page these rows are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
public sealed record InstancePage(
    IReadOnlyList<Instance> Rows, int Total, int Page, int TotalPages);
