using Memoria.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the streamed log itself: every event appended, or every one of a single stream, a single
/// type, or both.
/// </summary>
/// <remarks>
/// The streamed counterpart of <see cref="AppendedEvents"/>, and the same shape as it — narrowing,
/// counting, ordering and paging are all the database's, and only the reading of a payload is
/// shared. What is here and not there is the stream: DCB writes an event into one log and works out
/// afterwards which boundaries take it in, while a streamed event is appended to a named stream and
/// carries that name, so the stream is a column of the table and something to narrow it by.
/// </remarks>
public static class StreamedEvents
{
    /// <summary>
    /// Reads one page of the log.
    /// </summary>
    /// <param name="context">The streamed store.</param>
    /// <param name="streamPattern">
    /// The pattern the ids of one stream type match, or null for every stream. Worked out by
    /// <see cref="StreamShape"/>, because the log stores the id a stream produced and not the type
    /// that produced it — so narrowing to a type is matching the shape of what it writes.
    /// </param>
    /// <param name="eventType">The binding key to narrow to, or null for every type.</param>
    /// <param name="text">
    /// Text the row has to carry, in its stream id or in its payload, or null for any row.
    /// </param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// The stream and the sequence break a tie on the date: everything appended in one transaction
    /// is stamped from one clock reading and so shares a date exactly, and an unstable order under
    /// paging would show one row on two pages and another on none. The sequence alone would not
    /// settle it — it counts within a stream, so two streams both hold a first event — which is why
    /// the stream comes before it, and reads each stream's share of that moment in the order it was
    /// written.
    /// <para>
    /// The payload is matched as it was written, which is the serialized event whole — so the text
    /// looked for reaches the property names as well as the values under them. That is the point of
    /// it: the log is read here to find out what was appended, and a reader who knows only that an
    /// order carried a certain reference should not have to know which property holds it.
    /// </para>
    /// <para>
    /// The stream id is matched by the same text, which is why one box does for both. The stream
    /// type narrows to a kind of stream — every customer — and what a reader wants next is one of
    /// them, whose id they have in front of them in the first column; a second control to type it
    /// into would be a second way of asking the one question this box already asks. Either side
    /// matching is enough, because a reader typing an order reference means the payload and one
    /// typing <c>c-8d89</c> means the stream, and nothing is served by making them say which.
    /// </para>
    /// </remarks>
    public static async Task<StoredStreamEvents> Page(
        StreamedStoreDbContext context,
        string? streamPattern,
        string? eventType,
        string? text,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = context.Events.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(streamPattern))
            {
                // Like rather than a prefix comparison, because a stream's values are not always at
                // the end of its id: one built from two of them writes something after the first,
                // and the pattern has a hole in the middle to match it.
                stored = stored.Where(appended => EF.Functions.Like(appended.StreamId, streamPattern));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                stored = stored.Where(appended => appended.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Lowered on both sides rather than with a provider's case-insensitive operator, so
                // this reads the same against SQL Server as it does against Postgres. Contains
                // rather than the Like the stream pattern is matched by: this text was typed by
                // someone, and % and _ are ordinary characters they may well be looking for, so
                // Contains leaves the provider to escape them rather than reading them as
                // wildcards.
                var wanted = text.Trim().ToLower();

                stored = stored.Where(appended =>
                    appended.StreamId.ToLower().Contains(wanted) ||
                    appended.Data.ToLower().Contains(wanted));
            }

            var total = await stored.CountAsync(cancellationToken);
            var placed = InstanceQuery.Place(page, total, size);

            var ordered = descending
                ? stored.OrderByDescending(appended => appended.CreatedDate)
                    .ThenBy(appended => appended.StreamId)
                    .ThenByDescending(appended => appended.Sequence)
                : stored.OrderBy(appended => appended.CreatedDate)
                    .ThenBy(appended => appended.StreamId)
                    .ThenBy(appended => appended.Sequence);

            var rows = await ordered
                .Skip(placed.Skip)
                .Take(size)
                .Select(appended => new
                {
                    appended.StreamId, appended.Sequence, appended.EventType, appended.Data,
                    appended.CreatedDate
                })
                .ToListAsync(cancellationToken);

            // The same reading the DCB log and a boundary's events go through, so a row says the
            // same thing wherever it is met — including a row whose type the uploaded assemblies no
            // longer describe, which is listed rather than dropped. The sequence goes where a
            // position goes: both are the number that says where in the log a row sits, counted
            // within a stream here and across the whole log there.
            var read = rows
                .Select(row => new StoredStreamEvent(
                    row.StreamId,
                    BoundaryEvents.Read(row.Sequence, row.EventType, row.Data, row.CreatedDate)))
                .ToList();

            return new StoredStreamEvents(read, total, placed.Page, placed.TotalPages, Error: null);
        }
        catch (Exception exception)
        {
            return new StoredStreamEvents([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }
}

/// <summary>One page of the streamed log.</summary>
/// <param name="Events">Those on this page, in the order asked for.</param>
/// <param name="Total">How many there are, across every page.</param>
/// <param name="Page">The page these are, from one.</param>
/// <param name="TotalPages">How many pages there are, never fewer than one.</param>
/// <param name="Error">Why the log could not be read, or null when it was.</param>
public sealed record StoredStreamEvents(
    IReadOnlyList<StoredStreamEvent> Events, int Total, int Page, int TotalPages, string? Error);

/// <summary>
/// One appended event, and the stream it was appended to.
/// </summary>
/// <param name="StreamId">The stream it was written into.</param>
/// <param name="Event">
/// The event itself, read exactly as the DCB log reads one — so a payload, a type it cannot resolve
/// and a version are all shown the same way on both pages.
/// </param>
public sealed record StoredStreamEvent(string StreamId, StoredEvent Event);

/// <summary>
/// Which of the log's streams a page was asked for: all of them, the ones of a single stream type,
/// or neither.
/// </summary>
/// <param name="Stream">The type chosen, or null when none was or the name reached nothing.</param>
/// <param name="Unknown">Whether a name was asked for that is not among the registered types.</param>
/// <remarks>
/// The same shape as <see cref="EventChoice"/> and for the same reason: asking for nothing and
/// asking for something that is not there are two different questions with two different answers,
/// and both leave <see cref="Stream"/> null — so which was asked is kept beside it rather than
/// inferred from it. Answering an unknown name with the whole log would look like the filter had
/// been applied, and read as that stream holding events appended somewhere else.
/// <para>
/// Chosen by type rather than by the ids in the log, which is the difference from the event beside
/// it in the other direction: an event type is one key the log wrote, while a stream type is as
/// many ids as it has been given values — a customer apiece — so the ids are a list as long as the
/// log is wide and the type is the thing there are few enough of to choose from.
/// </para>
/// </remarks>
public sealed record StreamChoice(Type? Stream, bool Unknown)
{
    /// <summary>
    /// Reads the choice out of the name in the address.
    /// </summary>
    /// <param name="streams">
    /// The stream types the streams page lists, which is what the name is matched against — so a
    /// name arriving in a query string can only ever reach a type this application already knows
    /// about.
    /// </param>
    /// <param name="asked">The full name asked for, or null when the whole log was.</param>
    public static StreamChoice Of(IReadOnlyList<Type> streams, string? asked) =>
        string.IsNullOrWhiteSpace(asked)
            ? new StreamChoice(Stream: null, Unknown: false)
            : DomainTypeDescriber.Select(streams, asked) is { } found
                ? new StreamChoice(found, Unknown: false)
                : new StreamChoice(Stream: null, Unknown: true);

    /// <summary>Gets whether every stream is being read.</summary>
    public bool IsAll => Stream is null && !Unknown;

    /// <summary>
    /// Gets the pattern the ids of the chosen type match, or null when there is no single type to
    /// narrow to or its pattern cannot be worked out.
    /// </summary>
    public string? Pattern => Stream is null ? null : StreamShape.Of(Stream)?.Pattern;

    /// <summary>
    /// Gets whether the chosen type is one whose ids cannot be recognised, so there is no way to ask
    /// the log for them. The counterpart of <see cref="EventChoice.Unbound"/>: registered, listed,
    /// and impossible to narrow by.
    /// </summary>
    public bool Unshaped => Stream is not null && Pattern is null;

    /// <summary>
    /// Gets whether there are no rows to look for. Null narrows to every stream rather than to
    /// none, so a page asks this before it asks the store — the alternative is showing the whole log
    /// under the name of one stream type.
    /// </summary>
    public bool ShowsNothing => Unknown || Unshaped;

    /// <summary>
    /// Gets what is being read, for the line that counts what the table holds. The type as the
    /// streams page names it, rather than the pattern it happens to match by: the pattern is how the
    /// question is asked, not what was asked for.
    /// </summary>
    public string Name =>
        Stream is null
            ? Unknown ? "Streams" : "All streams"
            : DomainTypeDescriber.LabelOf(Stream);
}
