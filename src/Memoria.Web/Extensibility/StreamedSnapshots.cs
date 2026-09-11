using Memoria.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the snapshots the streamed store holds, of either kind of model: every one written, or
/// every one of a single stream type, a single model type, or both.
/// </summary>
/// <remarks>
/// The companion to <see cref="StreamedEvents"/> and the same shape as it — narrowing, counting,
/// ordering and paging are all the database's. What differs is what a row is: an event is one thing
/// that happened and has one date, while a snapshot is what a model had folded to when it was last
/// written, so it carries two dates, the version it reached and the sequence it had read up to.
/// <para>
/// Aggregates and projections are read by one method because the store keeps them in two tables of
/// the same columns: a snapshot of either is a model folded from a stream and written down, and the
/// only questions that differ are which table to read and which map turns a stored key back into a
/// type. Both are answered by <see cref="StreamedModelKind"/>.
/// </para>
/// <para>
/// Nothing is folded to draw a row, and the payload is not read at all: what the pages list is what
/// the store says about a snapshot — where it came from, what addressed it, what it was folded into,
/// how far it got and when — rather than what the snapshot holds.
/// </para>
/// </remarks>
public static class StreamedSnapshots
{
    /// <summary>
    /// Reads one page of the snapshots.
    /// </summary>
    /// <param name="context">The streamed store.</param>
    /// <param name="kind">Which of the two models to read.</param>
    /// <param name="streamPattern">
    /// The pattern the ids of one stream type match, or null for every stream. Worked out by
    /// <see cref="IdShape"/>, because the store holds the id a stream produced and not the type
    /// that produced it.
    /// </param>
    /// <param name="modelType">The binding key to narrow to, or null for every type.</param>
    /// <param name="identifierPattern">
    /// The pattern the ids of one identifier match, or null for every identifier. Worked out by
    /// <see cref="IdShape"/>, for the same reason the stream is: the store keeps the id a model was
    /// addressed by and not the identifier that produced it.
    /// </param>
    /// <param name="text">
    /// Text the row has to carry, in its stream id or its own id, or null for any row.
    /// </param>
    /// <param name="sort">Which of the two dates to order by.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// The stream and the id break a tie on the date, so paging is stable: several models written in
    /// one transaction share a date exactly, and an unstable order would show one row on two pages
    /// and another on none. The id alone would not settle it — a model is only unique within its
    /// stream — which is why the stream comes first.
    /// </remarks>
    public static async Task<StoredStreamSnapshots> Page(
        StreamedStoreDbContext context,
        StreamedModelKind kind,
        string? streamPattern,
        string? modelType,
        string? identifierPattern,
        string? text,
        InstanceSort sort,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = Rows(context, kind);

            if (!string.IsNullOrWhiteSpace(streamPattern))
            {
                stored = stored.Where(snapshot => EF.Functions.Like(snapshot.StreamId, streamPattern));
            }

            if (!string.IsNullOrWhiteSpace(modelType))
            {
                stored = stored.Where(snapshot => snapshot.Type == modelType);
            }

            if (!string.IsNullOrWhiteSpace(identifierPattern))
            {
                // The store writes the id and the type's version joined by a colon, so the pattern
                // is held against that whole key rather than against the id alone: an identifier
                // whose id ends in something fixed would otherwise match nothing at all.
                var wanted = $"{identifierPattern}:%";

                stored = stored.Where(snapshot => EF.Functions.Like(snapshot.StoreId, wanted));
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Lowered on both sides rather than with a provider's case-insensitive operator, so
                // this reads the same against SQL Server as it does against Postgres. Contains
                // rather than the Like the stream pattern is matched by: this text was typed by
                // someone, and % and _ are ordinary characters they may well be looking for.
                var wanted = text.Trim().ToLower();

                // The id as well as the stream, which is the one thing a snapshot has that an event
                // does not: it is addressed by an id of its own, and that is the column a reader is
                // most likely to be reading off when they type. The stored state is not looked in,
                // because neither page shows it: a row matching on something invisible would come
                // back with nothing on it carrying what was typed.
                stored = stored.Where(snapshot =>
                    snapshot.StreamId.ToLower().Contains(wanted) ||
                    snapshot.StoreId.ToLower().Contains(wanted));
            }

            var total = await stored.CountAsync(cancellationToken);
            var placed = InstanceQuery.Place(page, total, size);

            var ordered = (sort, descending) switch
            {
                (InstanceSort.Created, true) => stored.OrderByDescending(snapshot => snapshot.Created)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.StoreId),
                (InstanceSort.Created, false) => stored.OrderBy(snapshot => snapshot.Created)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.StoreId),
                (_, true) => stored.OrderByDescending(snapshot => snapshot.Updated)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.StoreId),
                (_, false) => stored.OrderBy(snapshot => snapshot.Updated)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.StoreId)
            };

            var rows = await ordered.Skip(placed.Skip).Take(size).ToListAsync(cancellationToken);

            var read = rows.Select(Read).ToList();

            return new StoredStreamSnapshots(read, total, placed.Page, placed.TotalPages, Error: null);
        }
        catch (Exception exception)
        {
            return new StoredStreamSnapshots([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }

    /// <summary>
    /// Reads the one model stored under an exact address, payload and all.
    /// </summary>
    /// <param name="context">The streamed store.</param>
    /// <param name="address">Which kind, in which stream, under which key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// What is stored there, or nothing when the address reaches no row — which is an answer rather
    /// than a fault: a stream can hold events no snapshot was ever written over.
    /// </returns>
    /// <remarks>
    /// Exact on both ids rather than narrowed by a pattern, because this is the key the store wrote
    /// the row under and a page about one model is about that row. The audit columns come back too,
    /// which the lists have no use for: who wrote a row is a fact about the one being read rather
    /// than a column to run an eye down.
    /// </remarks>
    public static async Task<ReadStreamModel> Model(
        StreamedStoreDbContext context,
        StreamedModelAddress address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = Whole(context, address.Kind).Where(snapshot =>
                snapshot.StreamId == address.StreamId && snapshot.StoreId == address.StoreId);

            var row = await stored.FirstOrDefaultAsync(cancellationToken);

            return new ReadStreamModel(row is null ? null : Read(row), Error: null);
        }
        catch (Exception exception)
        {
            return new ReadStreamModel(Snapshot: null, exception.Message);
        }
    }

    /// <summary>
    /// The rows of one kind with everything on them, as one shape for both tables.
    /// </summary>
    /// <remarks>
    /// The companion to <see cref="Rows"/> and read the same way, differing only in taking the
    /// payload and the audit columns as well — which is the difference between a page about one
    /// model and a list of many. By property rather than by constructor for the same reason: a
    /// provider follows a member it assigned itself back to the column it came from, and refuses a
    /// projection it cannot.
    /// </remarks>
    private static IQueryable<ModelRow> Whole(
        StreamedStoreDbContext context, StreamedModelKind kind) =>
        kind == StreamedModelKind.Projection
            ? context.Projections.AsNoTracking().Select(projection => new ModelRow
            {
                StreamId = projection.StreamId,
                StoreId = projection.Id,
                Type = projection.ProjectionType,
                Version = projection.Version,
                Sequence = projection.LatestEventSequence,
                Data = projection.Data,
                Created = projection.CreatedDate,
                CreatedBy = projection.CreatedBy,
                Updated = projection.UpdatedDate,
                UpdatedBy = projection.UpdatedBy
            })
            : context.Aggregates.AsNoTracking().Select(aggregate => new ModelRow
            {
                StreamId = aggregate.StreamId,
                StoreId = aggregate.Id,
                Type = aggregate.AggregateType,
                Version = aggregate.Version,
                Sequence = aggregate.LatestEventSequence,
                Data = aggregate.Data,
                Created = aggregate.CreatedDate,
                CreatedBy = aggregate.CreatedBy,
                Updated = aggregate.UpdatedDate,
                UpdatedBy = aggregate.UpdatedBy
            });

    /// <summary>One whole row of either table, in the columns the two have in common.</summary>
    private sealed class ModelRow : SnapshotRow
    {
        public string Data { get; init; } = string.Empty;

        public string? CreatedBy { get; init; }

        public string? UpdatedBy { get; init; }
    }

    /// <summary>One whole row as a page about a single model reads it.</summary>
    private static StoredStreamModel Read(ModelRow row) =>
        new(row.StreamId, row.StoreId, row.Type, row.Version, row.Sequence, row.Data,
            row.Created, row.CreatedBy, row.Updated, row.UpdatedBy);

    /// <summary>
    /// The rows of one kind, as the one shape both tables are read in.
    /// </summary>
    /// <remarks>
    /// Read into the shared shape before anything is asked of them, so the narrowing, the count, the
    /// order and the page are written once rather than once per table. The projection is by property
    /// rather than by constructor so the database still does all four: the provider follows a
    /// member it assigned itself back to the column it came from, and every Where and OrderBy below
    /// is translated rather than run here.
    /// </remarks>
    private static IQueryable<SnapshotRow> Rows(StreamedStoreDbContext context, StreamedModelKind kind) =>
        kind == StreamedModelKind.Projection
            ? context.Projections.AsNoTracking().Select(projection => new SnapshotRow
            {
                StreamId = projection.StreamId,
                StoreId = projection.Id,
                Type = projection.ProjectionType,
                Version = projection.Version,
                Sequence = projection.LatestEventSequence,
                Created = projection.CreatedDate,
                Updated = projection.UpdatedDate
            })
            : context.Aggregates.AsNoTracking().Select(aggregate => new SnapshotRow
            {
                StreamId = aggregate.StreamId,
                StoreId = aggregate.Id,
                Type = aggregate.AggregateType,
                Version = aggregate.Version,
                Sequence = aggregate.LatestEventSequence,
                Created = aggregate.CreatedDate,
                Updated = aggregate.UpdatedDate
            });

    /// <summary>One row of either table, in the columns the two have in common — which is all of them.</summary>
    private class SnapshotRow
    {
        public string StreamId { get; init; } = string.Empty;

        public string StoreId { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public int Version { get; init; }

        public int Sequence { get; init; }

        public DateTimeOffset Created { get; init; }

        public DateTimeOffset Updated { get; init; }
    }

    /// <summary>
    /// One row as the pages read it. Nothing is deserialized: both list what the store says about a
    /// snapshot — the stream it came from, the id it was addressed by, the type, the version and the
    /// dates — and none of that needs the payload opened. A row whose type the uploaded assemblies
    /// no longer describe is listed like any other, under the key the store wrote it as.
    /// </summary>
    private static StoredStreamSnapshot Read(SnapshotRow row) =>
        new(row.StreamId, row.StoreId, row.Type, row.Version, row.Sequence, row.Created, row.Updated);
}

/// <summary>One page of the streamed store's snapshots.</summary>
/// <param name="Snapshots">Those on this page, in the order asked for.</param>
/// <param name="Total">How many there are, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the store could not be read, or null when it was.</param>
public sealed record StoredStreamSnapshots(
    IReadOnlyList<StoredStreamSnapshot> Snapshots, int Total, int Page, int TotalPages, string? Error)
{
    /// <summary>
    /// Why this page is ordered more coarsely than it asked for, or null when it is not.
    /// </summary>
    /// <remarks>
    /// The same channel <see cref="StoredStreamEvents.OrderingNotice"/> carries, and for the same
    /// reason: the rows are real and one property of them is weaker. Only a store that cannot serve
    /// the full order sets it.
    /// </remarks>
    public string? OrderingNotice { get; init; }
}

/// <summary>One model, as the store holds it.</summary>
/// <param name="StreamId">The stream its events were appended to.</param>
/// <param name="StoreId">The key it was written under, as <c>id:version</c>.</param>
/// <param name="Type">The binding key its type was written under, as <c>name:version</c>.</param>
/// <param name="Version">How many events it had folded when it was written.</param>
/// <param name="Sequence">The sequence in its stream it had read up to.</param>
/// <param name="Created">When it was first written.</param>
/// <param name="Updated">When it was last written.</param>
public sealed record StoredStreamSnapshot(
    string StreamId,
    string StoreId,
    string Type,
    int Version,
    int Sequence,
    DateTimeOffset Created,
    DateTimeOffset Updated)
{
    /// <summary>
    /// Gets the id an identifier produced for it, which is the stored key without the type version
    /// the store appends.
    /// </summary>
    /// <remarks>
    /// Not what a page shows — the pages show the key as the store wrote it. This is what an
    /// identifier's pattern is held against, because a pattern describes the id the identifier
    /// makes and knows nothing of the version the store joins to it.
    /// </remarks>
    public string AddressedId => DomainTypeDescriber.SplitKey(StoreId).Name;

    /// <summary>Gets its type as every page here names one: what it is bound as, and the version.</summary>
    public string Name => DomainTypeDescriber.LabelOfKey(Type);
}

/// <summary>One model as the store holds it, read whole rather than listed.</summary>
/// <param name="StreamId">The stream its events were appended to.</param>
/// <param name="StoreId">The key it was written under, as <c>id:version</c>.</param>
/// <param name="Type">The binding key its type was written under, as <c>name:version</c>.</param>
/// <param name="Version">How many events it had folded when it was written.</param>
/// <param name="Sequence">The sequence in its stream it had read up to.</param>
/// <param name="Data">The payload, as the store wrote it.</param>
/// <param name="Created">When it was first written.</param>
/// <param name="CreatedBy">Who wrote it first, or null where the store attributes nothing.</param>
/// <param name="Updated">When it was last written.</param>
/// <param name="UpdatedBy">Who wrote it last, or null where the store attributes nothing.</param>
/// <remarks>
/// The list's <see cref="StoredStreamSnapshot"/> with the two things a page about one model needs
/// and a table of many has no use for: the payload, which is the state the page exists to show, and
/// the audit columns, which say who wrote the row a reader has stopped on.
/// </remarks>
public sealed record StoredStreamModel(
    string StreamId,
    string StoreId,
    string Type,
    int Version,
    int Sequence,
    string Data,
    DateTimeOffset Created,
    string? CreatedBy,
    DateTimeOffset Updated,
    string? UpdatedBy)
{
    /// <summary>Gets the id an identifier produced for it, without the version the store appends.</summary>
    public string AddressedId => DomainTypeDescriber.SplitKey(StoreId).Name;

    /// <summary>Gets its type as every page here names one: what it is bound as, and the version.</summary>
    public string Name => DomainTypeDescriber.LabelOfKey(Type);
}
