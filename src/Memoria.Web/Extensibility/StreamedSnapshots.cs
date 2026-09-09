using Memoria.EventSourcing;
using Memoria.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the snapshots the streamed store holds, of either kind of model: every one written, or
/// every one of a single stream type, a single model type, or both.
/// </summary>
/// <remarks>
/// The companion to <see cref="StreamedEvents"/> and the same shape as it — narrowing, counting,
/// ordering and paging are all the database's, and only the reading of the stored state is done
/// here. What differs is what a row is: an event is one thing that happened and has one date, while
/// a snapshot is what a model had folded to when it was last written, so it carries two dates, the
/// version it reached and the sequence it had read up to.
/// <para>
/// Aggregates and projections are read by one method because the store keeps them in two tables of
/// the same columns: a snapshot of either is a model folded from a stream and written down, and the
/// only questions that differ are which table to read and which map turns a stored key back into a
/// type. Both are answered by <see cref="StreamedModelKind"/>.
/// </para>
/// <para>
/// Nothing is folded to draw a row. The state shown is the snapshot as it was stored, which is the
/// only thing the store can answer without replaying the stream.
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
    /// <see cref="StreamShape"/>, because the store holds the id a stream produced and not the type
    /// that produced it.
    /// </param>
    /// <param name="modelType">The binding key to narrow to, or null for every type.</param>
    /// <param name="text">
    /// Text the row has to carry, in its stream id, its own id or its stored state, or null for any
    /// row.
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

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Lowered on both sides rather than with a provider's case-insensitive operator, so
                // this reads the same against SQL Server as it does against Postgres. Contains
                // rather than the Like the stream pattern is matched by: this text was typed by
                // someone, and % and _ are ordinary characters they may well be looking for.
                var wanted = text.Trim().ToLower();

                // The id as well as the stream and the state, which is the one thing this page has
                // that the events page does not: a snapshot is addressed by its own id, and it is
                // the column a reader is most likely to be reading off when they type.
                stored = stored.Where(snapshot =>
                    snapshot.StreamId.ToLower().Contains(wanted) ||
                    snapshot.StoreId.ToLower().Contains(wanted) ||
                    snapshot.Data.ToLower().Contains(wanted));
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

            var read = rows.Select(row => Read(kind, row)).ToList();

            return new StoredStreamSnapshots(read, total, placed.Page, placed.TotalPages, Error: null);
        }
        catch (Exception exception)
        {
            return new StoredStreamSnapshots([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }

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
                Data = projection.Data,
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
                Data = aggregate.Data,
                Created = aggregate.CreatedDate,
                Updated = aggregate.UpdatedDate
            });

    /// <summary>One row of either table, in the columns the two have in common — which is all of them.</summary>
    private sealed class SnapshotRow
    {
        public string StreamId { get; init; } = string.Empty;

        public string StoreId { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public int Version { get; init; }

        public int Sequence { get; init; }

        public string Data { get; init; } = string.Empty;

        public DateTimeOffset Created { get; init; }

        public DateTimeOffset Updated { get; init; }
    }

    /// <summary>
    /// Reads one stored snapshot back into the state it holds.
    /// </summary>
    /// <remarks>
    /// The same way <see cref="BoundaryEvents.Read"/> reads a payload, and for the same reason: a
    /// row whose type the uploaded assemblies no longer describe is listed with what could not be
    /// done to it, rather than dropped. The store's own facts about it — the stream, the id, the
    /// version, the dates — hold whatever the types turn out to say.
    /// </remarks>
    private static StoredStreamSnapshot Read(StreamedModelKind kind, SnapshotRow row)
    {
        var stored = new StoredStreamSnapshot(row.StreamId, row.StoreId, row.Type, row.Version,
            row.Sequence, row.Created, row.Updated, [], null);

        if (!kind.Bindings().TryGetValue(row.Type, out var clrType))
        {
            return stored with { Error = $"No uploaded type is registered as {row.Type}." };
        }

        try
        {
            var model = DomainSerializer.Current.Deserialize(row.Data, clrType);

            return model is null
                ? stored with { Error = "The stored snapshot is empty." }
                : stored with { State = DomainTypeDescriber.ReadState(model) };
        }
        catch (Exception exception)
        {
            return stored with { Error = exception.Message };
        }
    }
}

/// <summary>One page of the streamed store's snapshots.</summary>
/// <param name="Snapshots">Those on this page, in the order asked for.</param>
/// <param name="Total">How many there are, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the store could not be read, or null when it was.</param>
public sealed record StoredStreamSnapshots(
    IReadOnlyList<StoredStreamSnapshot> Snapshots, int Total, int Page, int TotalPages, string? Error);

/// <summary>One model, as the store holds it.</summary>
/// <param name="StreamId">The stream its events were appended to.</param>
/// <param name="StoreId">The key it was written under, as <c>id:version</c>.</param>
/// <param name="Type">The binding key its type was written under, as <c>name:version</c>.</param>
/// <param name="Version">How many events it had folded when it was written.</param>
/// <param name="Sequence">The sequence in its stream it had read up to.</param>
/// <param name="Created">When it was first written.</param>
/// <param name="Updated">When it was last written.</param>
/// <param name="State">What the snapshot holds, or empty when that could not be read.</param>
/// <param name="Error">Why it could not be read, or null when it was.</param>
public sealed record StoredStreamSnapshot(
    string StreamId,
    string StoreId,
    string Type,
    int Version,
    int Sequence,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    IReadOnlyList<DomainPropertyValue> State,
    string? Error)
{
    /// <summary>
    /// Gets the id it is addressed by, without the type version the store keeps beside it.
    /// </summary>
    /// <remarks>
    /// The store joins the two because something has to read them back apart, which is its need and
    /// not the reader's — and the version is already said in the type column, where it belongs to
    /// the name it versions.
    /// </remarks>
    public string Id => DomainTypeDescriber.SplitKey(StoreId).Name;

    /// <summary>Gets its type as every page here names one: what it is bound as, and the version.</summary>
    public string Name => DomainTypeDescriber.LabelOfKey(Type);
}
