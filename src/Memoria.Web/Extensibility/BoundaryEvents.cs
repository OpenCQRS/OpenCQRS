using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the events an aggregate applies out of the boundary it is folded from.
/// </summary>
/// <remarks>
/// Which rows are inside a boundary is the store's own question — one correlated <c>EXISTS</c> over
/// each event's tags, and different again for an intersection — so the selection comes from
/// <c>GetEventEntities</c> rather than being written a second time here. What is added is the
/// narrowing to the aggregate's own filter, the ordering and paging over what that leaves, and the
/// reading: the log stores a binding key and a payload, and the page wants the event those name.
/// <para>
/// These are the events the aggregate is built from, so the count agrees with the version a fold of
/// them would reach. A boundary may hold others — a wider model's events, sharing a tag — and those
/// are not listed here, because they are not what this aggregate is made of.
/// </para>
/// </remarks>
public static class BoundaryEvents
{
    /// <summary>
    /// Reads one page of the events an aggregate applies inside a boundary.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="boundary">The consistency boundary.</param>
    /// <param name="applies">The event types the aggregate applies, or null for all of them.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Ordered and paged in memory rather than in the database, because the store reads a boundary
    /// whole — that is what a fold needs, and it offers no first-<c>n</c> read to ask for instead.
    /// The size of one aggregate's history is what makes that affordable, and it is what makes the
    /// total exact rather than an estimate.
    /// </remarks>
    public static async Task<StoredEvents> Load(
        IDcbDbContext context,
        TagQuery boundary,
        Type[]? applies,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Page(await context.GetEventEntities(boundary, applies, cancellationToken),
                descending, page, size);
        }
        catch (Exception exception)
        {
            return new StoredEvents([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }

    /// <summary>
    /// The event types an aggregate applies.
    /// </summary>
    /// <param name="aggregate">The aggregate type.</param>
    /// <param name="loaded">One already read, or null to build a fresh one to ask.</param>
    /// <returns>The types, or null when it applies every event inside its boundary.</returns>
    /// <remarks>
    /// The filter is an instance property, so something has to exist to be asked. The one already
    /// read answers for itself; failing that a fresh one is built, which is sound because the filter
    /// is a property of the type rather than of any state it holds.
    /// <para>
    /// A type that cannot be built — no parameterless constructor, which the store's own reads
    /// require and so would fail on too — narrows nothing rather than narrowing to nothing. Showing
    /// every event in the boundary is the wrong answer in a way a reader can see; showing none is
    /// the wrong answer in a way that looks like an empty log.
    /// </para>
    /// </remarks>
    public static Type[]? AppliedBy(Type aggregate, object? loaded)
    {
        if (loaded is EventSourcedModel model)
        {
            return model.EventTypeFilter;
        }

        try
        {
            return InstanceFactory.CreateInstance(aggregate) as EventSourcedModel is { } fresh
                ? fresh.EventTypeFilter
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Orders stored rows by when they were appended and takes one page of them.
    /// </summary>
    /// <param name="rows">The rows inside the boundary.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <remarks>
    /// Position breaks a tie on the date. Events appended in one transaction are stamped from one
    /// clock reading and so share a date exactly, and an unstable order under paging would show one
    /// row on two pages and another on none.
    /// </remarks>
    public static StoredEvents Page(IReadOnlyList<DcbEventEntity> rows, bool descending, int page, int size)
    {
        var placed = InstanceQuery.Place(page, rows.Count, size);

        var ordered = descending
            ? rows.OrderByDescending(row => row.CreatedDate).ThenByDescending(row => row.Position)
            : rows.OrderBy(row => row.CreatedDate).ThenBy(row => row.Position);

        var read = ordered
            .Skip(placed.Skip)
            .Take(size)
            .Select(row => Read(row.Position, row.EventType, row.Data, row.CreatedDate))
            .ToList();

        return new StoredEvents(read, rows.Count, placed.Page, placed.TotalPages, Error: null);
    }

    /// <summary>
    /// Turns one stored row into the event it was written from.
    /// </summary>
    /// <param name="position">The event's global position.</param>
    /// <param name="eventType">The binding key it was stored under, as <c>name:version</c>.</param>
    /// <param name="data">The stored payload.</param>
    /// <param name="written">When it was appended.</param>
    /// <remarks>
    /// A row whose type is not registered, or whose payload will not read back, is still listed. The
    /// position, the type and the date are facts of the log itself and hold whatever the payload
    /// turns out to be — and an event the uploaded assemblies no longer describe is worth seeing
    /// rather than quietly dropping from a boundary it is genuinely inside.
    /// </remarks>
    public static StoredEvent Read(long position, string eventType, string data, DateTimeOffset written)
    {
        if (!TypeBindings.EventTypeBindings.TryGetValue(eventType, out var clrType))
        {
            return new StoredEvent(position, eventType, written, [],
                $"No uploaded type is registered as {eventType}.");
        }

        try
        {
            var @event = DomainSerializer.Current.Deserialize(data, clrType);

            return @event is null
                ? new StoredEvent(position, eventType, written, [], "The stored payload is empty.")
                : new StoredEvent(position, eventType, written, DomainTypeDescriber.ReadState(@event), null);
        }
        catch (Exception exception)
        {
            return new StoredEvent(position, eventType, written, [], exception.Message);
        }
    }
}

/// <summary>One page of the events read out of a boundary.</summary>
/// <param name="Events">Those on this page, in the order asked for.</param>
/// <param name="Total">How many the aggregate applies, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the log could not be read, or null when it was.</param>
public sealed record StoredEvents(
    IReadOnlyList<StoredEvent> Events, int Total, int Page, int TotalPages, string? Error);

/// <summary>One event, as the log holds it.</summary>
/// <param name="Position">Its global position.</param>
/// <param name="Type">The binding key it was stored under.</param>
/// <param name="Written">When it was appended.</param>
/// <param name="State">What its payload holds, or empty when that could not be read.</param>
/// <param name="Error">Why its payload could not be read, or null when it was.</param>
public sealed record StoredEvent(
    long Position,
    string Type,
    DateTimeOffset Written,
    IReadOnlyList<DomainPropertyValue> State,
    string? Error)
{
    /// <summary>Gets the name half of the key: what the type is written under.</summary>
    public string Name => Split(Type).Name;

    /// <summary>
    /// Gets the version half of the key, or null when the key carries none — a row is listed
    /// whatever its type string turns out to be, and one without a version is a name with nothing
    /// to say beside it rather than a row that cannot be drawn.
    /// </summary>
    public string? Version => Split(Type).Version;

    /// <summary>
    /// Splits a key at its last separator. A key is written as <c>name:version</c> with nothing
    /// escaped, so a name carrying a colon of its own still leaves the version after the last one.
    /// </summary>
    private static (string Name, string? Version) Split(string key) =>
        key.LastIndexOf(':') is var separator && separator < 0
            ? (key, null)
            : (key[..separator], key[(separator + 1)..]);
}
