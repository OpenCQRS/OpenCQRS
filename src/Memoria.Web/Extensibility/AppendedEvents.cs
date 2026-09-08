using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the log itself: every event appended, or every one of a single type, whatever boundary it
/// was written under.
/// </summary>
/// <remarks>
/// The companion to <see cref="BoundaryEvents"/>, and the other way round from it. There the
/// question is which events one model is folded from, so the store resolves a boundary and the rows
/// it returns are ordered and paged in memory. Here there is no boundary to resolve — the rows
/// wanted are the whole table, or the part of it carrying one type — so narrowing, counting,
/// ordering and paging are all the database's, and only the reading of a payload is shared.
/// <para>
/// Narrowing by type is a scan: the store carries no index on <c>EventType</c>, deliberately, so
/// that the optimiser cannot prefer one to the tag semi-join every real read goes through. Paying
/// for it here is the right way round — this is a page someone opened to look at the log, not a
/// read on the write path.
/// </para>
/// </remarks>
public static class AppendedEvents
{
    /// <summary>
    /// Reads one page of the log.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="eventType">The binding key to narrow to, or null for every type.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Position breaks a tie on the date, as it does in a boundary's events: everything appended in
    /// one transaction is stamped from one clock reading and so shares a date exactly, and an
    /// unstable order under paging would show one row on two pages and another on none.
    /// </remarks>
    public static async Task<StoredEvents> Page(
        IDcbDbContext context,
        string? eventType,
        bool descending,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = context.DcbEvents.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                stored = stored.Where(appended => appended.EventType == eventType);
            }

            var total = await stored.CountAsync(cancellationToken);
            var placed = InstanceQuery.Place(page, total, size);

            var ordered = descending
                ? stored.OrderByDescending(appended => appended.CreatedDate)
                    .ThenByDescending(appended => appended.Position)
                : stored.OrderBy(appended => appended.CreatedDate)
                    .ThenBy(appended => appended.Position);

            var rows = await ordered
                .Skip(placed.Skip)
                .Take(size)
                .Select(appended => new
                {
                    appended.Position, appended.EventType, appended.Data, appended.CreatedDate
                })
                .ToListAsync(cancellationToken);

            // The same reading a boundary's events go through, so a row says the same thing
            // wherever it is met — including a row whose type the uploaded assemblies no longer
            // describe, which is listed rather than dropped.
            var read = rows
                .Select(row => BoundaryEvents.Read(row.Position, row.EventType, row.Data, row.CreatedDate))
                .ToList();

            return new StoredEvents(read, total, placed.Page, placed.TotalPages, Error: null);
        }
        catch (Exception exception)
        {
            return new StoredEvents([], Total: 0, Page: 1, TotalPages: 1, Error: exception.Message);
        }
    }
}

/// <summary>
/// Which of the log's events a page was asked for: all of them, one type, or neither.
/// </summary>
/// <param name="Event">The type chosen, or null when none was or the name reached nothing.</param>
/// <param name="Unknown">Whether a name was asked for that is not among the registered types.</param>
/// <remarks>
/// Asking for nothing and asking for something that is not there are two different questions with
/// two different answers, and both leave <see cref="Event"/> null — so which was asked is kept
/// beside it rather than inferred from it. Answering an unknown name with the whole log would look
/// like the filter had been applied, and read as the log holding events it does not.
/// </remarks>
public sealed record EventChoice(Type? Event, bool Unknown)
{
    /// <summary>
    /// Reads the choice out of the name in the address.
    /// </summary>
    /// <param name="events">The types the events page lists, which is what the name is matched
    /// against — so a name arriving in a query string can only ever reach a type this application
    /// already knows about, and only one a DCB model applies.</param>
    /// <param name="asked">The full name asked for, or null when the whole log was.</param>
    public static EventChoice Of(IReadOnlyList<Type> events, string? asked) =>
        string.IsNullOrWhiteSpace(asked)
            ? new EventChoice(Event: null, Unknown: false)
            : DomainTypeDescriber.Select(events, asked) is { } found
                ? new EventChoice(found, Unknown: false)
                : new EventChoice(Event: null, Unknown: true);

    /// <summary>Gets whether the whole log is being read.</summary>
    public bool IsAll => Event is null && !Unknown;

    /// <summary>
    /// Gets the key the log wrote the chosen type under, as <c>name:version</c>, or null when there
    /// is no single type to narrow to.
    /// </summary>
    public string? Key => Event is null ? null : DomainTypeDescriber.BindingOf(Event)?.Key;

    /// <summary>
    /// Gets whether the chosen type carries no <c>[EventType]</c>, so the log was never able to
    /// write one of it.
    /// </summary>
    public bool Unbound => Event is not null && Key is null;

    /// <summary>
    /// Gets whether there are no rows to look for. Null narrows to nothing rather than to
    /// everything, so a page asks this before it asks the store — the alternative is showing the
    /// whole log under the name of a type it holds none of.
    /// </summary>
    public bool ShowsNothing => Unknown || Unbound;

    /// <summary>
    /// Gets what is being read, for the heading and the breadcrumb. A chosen type is named as the
    /// log names it, falling back to the class when it is bound by nothing.
    /// </summary>
    public string Name =>
        Event is null
            ? Unknown ? "Events" : "All events"
            : DomainTypeDescriber.LabelOf(Event);
}
