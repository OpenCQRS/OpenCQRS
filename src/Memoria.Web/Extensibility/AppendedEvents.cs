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
    /// <param name="payload">Text the stored payload has to carry, or null for any payload.</param>
    /// <param name="descending">Whether the newest come first.</param>
    /// <param name="page">The page asked for, from one.</param>
    /// <param name="size">The rows per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Position breaks a tie on the date, as it does in a boundary's events: everything appended in
    /// one transaction is stamped from one clock reading and so shares a date exactly, and an
    /// unstable order under paging would show one row on two pages and another on none.
    /// <para>
    /// The payload is matched as it was written, which is the serialized event whole — so the text
    /// looked for reaches the property names as well as the values under them. That is the point of
    /// it: the log is read here to find out what was appended, and a reader who knows only that an
    /// order carried a certain reference should not have to know which property holds it.
    /// </para>
    /// </remarks>
    public static async Task<StoredEvents> Page(
        IDcbDbContext context,
        string? eventType,
        string? payload,
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

            if (!string.IsNullOrWhiteSpace(payload))
            {
                // Lowered on both sides rather than with a provider's case-insensitive operator, so
                // this reads the same against SQL Server as it does against Postgres. Contains
                // rather than the Like the tag filter is built on: this text is typed against a
                // payload, where % and _ are ordinary characters someone may well be looking for,
                // and Contains leaves the provider to escape them rather than reading them as
                // wildcards.
                var wanted = payload.Trim().ToLower();

                stored = stored.Where(appended => appended.Data.ToLower().Contains(wanted));
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
