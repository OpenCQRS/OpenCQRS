using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the events an aggregate or a projection applies out of the boundary it is folded from.
/// </summary>
/// <remarks>
/// Which rows are inside a boundary is the store's own question — one correlated <c>EXISTS</c> over
/// each event's tags, and different again for an intersection — so the selection comes from
/// <c>GetEventEntities</c> rather than being written a second time here. What is added is the
/// narrowing to the model's own filter, the ordering and paging over what that leaves, and the
/// reading: the log stores a binding key and a payload, and the page wants the event those name.
/// <para>
/// These are the events the model is built from, so the count agrees with the version a fold of
/// them would reach. A boundary may hold others — a wider model's events, sharing a tag — and those
/// are not listed here, because they are not what this model is made of.
/// </para>
/// </remarks>
public static class BoundaryEvents
{
    /// <summary>
    /// Reads one page of the events a model applies inside a boundary.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="boundary">The consistency boundary.</param>
    /// <param name="applies">The event types the model applies, or null for all of them.</param>
    /// <param name="eventType">The binding key to narrow to, or null for every type it applies.</param>
    /// <param name="text">Text the row's payload has to carry, or null for any row.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Ordered and paged in memory rather than in the database, because the store reads a boundary
    /// whole — that is what a fold needs, and it offers no first-<c>n</c> read to ask for instead.
    /// The size of one model's history is what makes that affordable, and it is what makes the
    /// total exact rather than an estimate.
    /// <para>
    /// A reader's narrowing goes the same way, and for the same reason: the rows are already in
    /// hand, so asking the store a second, narrower question would cost a round trip to answer what
    /// a <c>Where</c> over what it just returned answers. <paramref name="applies"/> stays the
    /// store's, though — which events the model is made of is what the boundary read selects on,
    /// and it is not a reader's to widen.
    /// </para>
    /// </remarks>
    public static async Task<StoredEvents> Load(
        IDcbDbContext context,
        TagQuery boundary,
        Type[]? applies,
        string? eventType,
        string? text,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Page(await context.GetEventEntities(boundary, applies, cancellationToken),
                eventType, text, descending, page, size);
        }
        catch (Exception exception)
        {
            return new StoredEvents([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }

    /// <summary>
    /// The event types a model applies.
    /// </summary>
    /// <param name="model">The aggregate or projection type.</param>
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
    public static Type[]? AppliedBy(Type model, object? loaded)
    {
        if (loaded is EventSourcedModel already)
        {
            return already.EventTypeFilter;
        }

        try
        {
            return InstanceFactory.CreateInstance(model) as EventSourcedModel is { } fresh
                ? fresh.EventTypeFilter
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Narrows stored rows to what a reader asked for, orders them by when they were appended, and
    /// takes one page of what is left.
    /// </summary>
    /// <param name="rows">The rows inside the boundary.</param>
    /// <param name="eventType">The binding key to narrow to, or null for every type.</param>
    /// <param name="text">Text the row's payload has to carry, or null for any row.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <remarks>
    /// Narrowed before it is counted, so the count and the pager answer for what a reader asked for
    /// rather than for what the boundary holds.
    /// <para>
    /// Position breaks a tie on the date. Events appended in one transaction are stamped from one
    /// clock reading and so share a date exactly, and an unstable order under paging would show one
    /// row on two pages and another on none.
    /// </para>
    /// </remarks>
    public static StoredEvents Page(
        IReadOnlyList<DcbEventEntity> rows,
        string? eventType,
        string? text,
        bool descending,
        int page,
        int size)
    {
        var matching = Matching(rows, eventType, text);
        var placed = InstanceQuery.Place(page, matching.Count, size);

        var ordered = descending
            ? matching.OrderByDescending(row => row.CreatedDate).ThenByDescending(row => row.Position)
            : matching.OrderBy(row => row.CreatedDate).ThenBy(row => row.Position);

        var read = ordered
            .Skip(placed.Skip)
            .Take(size)
            .Select(row => Read(row.Position, row.EventType, row.Data, row.CreatedDate))
            .ToList();

        return new StoredEvents(read, matching.Count, placed.Page, placed.TotalPages, Error: null);
    }

    /// <summary>
    /// The rows a reader's two narrowings leave: the type the log wrote them under, and the text
    /// they carry.
    /// </summary>
    /// <remarks>
    /// Both narrow the same list, so a row has to answer both. The type is matched on the key rather
    /// than on a CLR type, because the key is what the row holds — and a row whose type the uploaded
    /// assemblies no longer describe still has one.
    /// <para>
    /// The payload is matched as it was written, which is the serialized event whole, so the text
    /// looked for reaches the property names as well as the values under them. That is the point of
    /// it: a reader who knows only that an event carried a certain reference should not have to know
    /// which property holds it.
    /// </para>
    /// <para>
    /// The payload and nothing else. The position is not asked, unlike on the log's own page, and a
    /// number typed here is text like any other: a history is short enough to run an eye down the
    /// positions of, so a box that also matched them would answer a reference that happens to be
    /// numeric with a row that merely sits at that number.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<DcbEventEntity> Matching(
        IReadOnlyList<DcbEventEntity> rows, string? eventType, string? text)
    {
        var narrowed = string.IsNullOrWhiteSpace(eventType)
            ? rows
            : rows.Where(row => row.EventType == eventType).ToList();

        if (string.IsNullOrWhiteSpace(text))
        {
            return narrowed;
        }

        var wanted = text.Trim();

        return narrowed
            .Where(row => row.Data.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Turns one stored row into the event it was written from.
    /// </summary>
    /// <param name="position">The event's global position.</param>
    /// <param name="eventType">The binding key it was stored under, as <c>name:version</c>.</param>
    /// <param name="data">The stored payload.</param>
    /// <param name="written">When it was appended.</param>
    /// <param name="tags">
    /// The tags it was appended under, or null when the read did not ask for them — a streamed event
    /// has none, and a boundary's own read selects through them rather than loading them.
    /// </param>
    /// <remarks>
    /// A row whose type is not registered, or whose payload will not read back, is still listed. The
    /// position, the type and the date are facts of the log itself and hold whatever the payload
    /// turns out to be — and an event the uploaded assemblies no longer describe is worth seeing
    /// rather than quietly dropping from a boundary it is genuinely inside.
    /// <para>
    /// The tags are among those facts, and are kept whatever the payload turns out to be for the
    /// same reason: they are what the log wrote the row under, and a row nothing here can read back
    /// is exactly the one a reader wants the tags of.
    /// </para>
    /// </remarks>
    public static StoredEvent Read(long position, string eventType, string data, DateTimeOffset written,
        IReadOnlyList<string>? tags = null)
    {
        var under = tags ?? [];

        if (!TypeBindings.EventTypeBindings.TryGetValue(eventType, out var clrType))
        {
            return new StoredEvent(position, eventType, written, data, [],
                $"No uploaded type is registered as {eventType}.", under);
        }

        try
        {
            var @event = DomainSerializer.Current.Deserialize(data, clrType);

            return @event is null
                ? new StoredEvent(position, eventType, written, data, [], "The stored payload is empty.", under)
                : new StoredEvent(position, eventType, written, data, DomainTypeDescriber.ReadState(@event),
                    null, under);
        }
        catch (Exception exception)
        {
            return new StoredEvent(position, eventType, written, data, [], exception.Message, under);
        }
    }
}

/// <summary>One page of the events read out of a boundary.</summary>
/// <param name="Events">Those on this page, in the order asked for.</param>
/// <param name="Total">How many the model applies, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the log could not be read, or null when it was.</param>
public sealed record StoredEvents(
    IReadOnlyList<StoredEvent> Events, int Total, int Page, int TotalPages, string? Error);

/// <summary>One event, as the log holds it.</summary>
/// <param name="Position">Its global position.</param>
/// <param name="Type">The binding key it was stored under.</param>
/// <param name="Written">When it was appended.</param>
/// <param name="Data">
/// Its payload, as the log wrote it. Kept beside the properties it was read into, because the Json
/// column shows the row's own text rather than the event serialised again — and shows it whatever
/// became of the read, since a type nothing uploaded describes and a payload that will not open are
/// exactly the rows worth looking at.
/// </param>
/// <param name="State">What its payload holds, or empty when that could not be read.</param>
/// <param name="Error">Why its payload could not be read, or null when it was.</param>
/// <param name="Tags">
/// The tags it was appended under, or empty when the read did not ask for them. Empty is not the
/// claim that it carries none: a streamed event has no tags to carry, and a boundary's own read
/// selects through the tag table rather than loading it, so only a read that asks — the log itself,
/// where the tags are what a row would otherwise be reached by and never say — fills this.
/// </param>
public sealed record StoredEvent(
    long Position,
    string Type,
    DateTimeOffset Written,
    string Data,
    IReadOnlyList<DomainPropertyValue> State,
    string? Error,
    IReadOnlyList<string> Tags)
{
    /// <summary>Gets the name half of the key: what the type is written under.</summary>
    public string Name => DomainTypeDescriber.SplitKey(Type).Name;

    /// <summary>
    /// Gets the version half of the key, or null when the key carries none — a row is listed
    /// whatever its type string turns out to be, and one without a version is a name with nothing
    /// to say beside it rather than a row that cannot be drawn.
    /// </summary>
    public string? Version => DomainTypeDescriber.SplitKey(Type).Version;
}
