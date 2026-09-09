using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the snapshots the streamed store holds: every aggregate written, or every one of a single
/// stream type, a single aggregate type, or both.
/// </summary>
/// <remarks>
/// The companion to <see cref="StreamedEvents"/> and the same shape as it — narrowing, counting,
/// ordering and paging are all the database's, and only the reading of the stored state is done
/// here. What differs is what a row is: an event is one thing that happened and has one date, while
/// a snapshot is what a model had folded to when it was last written, so it carries two dates, the
/// version it reached and the sequence it had read up to.
/// <para>
/// Nothing is folded to draw a row. The state shown is the snapshot as it was stored, which is the
/// only thing the store can answer without replaying the stream.
/// </para>
/// </remarks>
public static class StreamedAggregates
{
    /// <summary>
    /// Reads one page of the snapshots.
    /// </summary>
    /// <param name="context">The streamed store.</param>
    /// <param name="streamPattern">
    /// The pattern the ids of one stream type match, or null for every stream. Worked out by
    /// <see cref="StreamShape"/>, because the store holds the id a stream produced and not the type
    /// that produced it.
    /// </param>
    /// <param name="aggregateType">The binding key to narrow to, or null for every type.</param>
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
    /// and another on none. The id alone would not settle it — an aggregate is only unique within
    /// its stream — which is why the stream comes first.
    /// </remarks>
    public static async Task<StoredAggregates> Page(
        StreamedStoreDbContext context,
        string? streamPattern,
        string? aggregateType,
        string? text,
        InstanceSort sort,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = context.Aggregates.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(streamPattern))
            {
                stored = stored.Where(snapshot => EF.Functions.Like(snapshot.StreamId, streamPattern));
            }

            if (!string.IsNullOrWhiteSpace(aggregateType))
            {
                stored = stored.Where(snapshot => snapshot.AggregateType == aggregateType);
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
                    snapshot.Id.ToLower().Contains(wanted) ||
                    snapshot.Data.ToLower().Contains(wanted));
            }

            var total = await stored.CountAsync(cancellationToken);
            var placed = InstanceQuery.Place(page, total, size);

            var ordered = (sort, descending) switch
            {
                (InstanceSort.Created, true) => stored.OrderByDescending(snapshot => snapshot.CreatedDate)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.Id),
                (InstanceSort.Created, false) => stored.OrderBy(snapshot => snapshot.CreatedDate)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.Id),
                (_, true) => stored.OrderByDescending(snapshot => snapshot.UpdatedDate)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.Id),
                (_, false) => stored.OrderBy(snapshot => snapshot.UpdatedDate)
                    .ThenBy(snapshot => snapshot.StreamId).ThenBy(snapshot => snapshot.Id)
            };

            var rows = await ordered
                .Skip(placed.Skip)
                .Take(size)
                .Select(snapshot => new
                {
                    snapshot.StreamId, snapshot.Id, snapshot.AggregateType, snapshot.Version,
                    snapshot.LatestEventSequence, snapshot.Data, snapshot.CreatedDate,
                    snapshot.UpdatedDate
                })
                .ToListAsync(cancellationToken);

            var read = rows
                .Select(row => Read(
                    row.StreamId, row.Id, row.AggregateType, row.Version, row.LatestEventSequence,
                    row.Data, row.CreatedDate, row.UpdatedDate))
                .ToList();

            return new StoredAggregates(read, total, placed.Page, placed.TotalPages, Error: null);
        }
        catch (Exception exception)
        {
            return new StoredAggregates([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
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
    private static StoredAggregate Read(
        string streamId,
        string id,
        string aggregateType,
        int version,
        int sequence,
        string data,
        DateTimeOffset created,
        DateTimeOffset updated)
    {
        if (!TypeBindings.AggregateTypeBindings.TryGetValue(aggregateType, out var clrType))
        {
            return new StoredAggregate(streamId, id, aggregateType, version, sequence, created,
                updated, [], $"No uploaded type is registered as {aggregateType}.");
        }

        try
        {
            var aggregate = DomainSerializer.Current.Deserialize(data, clrType);

            return aggregate is null
                ? new StoredAggregate(streamId, id, aggregateType, version, sequence, created,
                    updated, [], "The stored snapshot is empty.")
                : new StoredAggregate(streamId, id, aggregateType, version, sequence, created,
                    updated, DomainTypeDescriber.ReadState(aggregate), null);
        }
        catch (Exception exception)
        {
            return new StoredAggregate(streamId, id, aggregateType, version, sequence, created,
                updated, [], exception.Message);
        }
    }
}

/// <summary>One page of the streamed store's snapshots.</summary>
/// <param name="Aggregates">Those on this page, in the order asked for.</param>
/// <param name="Total">How many there are, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the store could not be read, or null when it was.</param>
public sealed record StoredAggregates(
    IReadOnlyList<StoredAggregate> Aggregates, int Total, int Page, int TotalPages, string? Error);

/// <summary>One aggregate, as the store holds it.</summary>
/// <param name="StreamId">The stream its events were appended to.</param>
/// <param name="StoreId">The key it was written under, as <c>id:version</c>.</param>
/// <param name="Type">The binding key its type was written under, as <c>name:version</c>.</param>
/// <param name="Version">How many events it had folded when it was written.</param>
/// <param name="Sequence">The sequence in its stream it had read up to.</param>
/// <param name="Created">When it was first written.</param>
/// <param name="Updated">When it was last written.</param>
/// <param name="State">What the snapshot holds, or empty when that could not be read.</param>
/// <param name="Error">Why it could not be read, or null when it was.</param>
public sealed record StoredAggregate(
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

    /// <summary>Gets the name half of the type's key: what the type is written under.</summary>
    public string Name => DomainTypeDescriber.LabelOfKey(Type);
}
