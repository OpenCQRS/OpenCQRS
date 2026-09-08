using Memoria.Web.Samples.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Empties the tables, so a run can start from nothing.
/// </summary>
/// <remarks>
/// Straight at the tables rather than through <see cref="IDomainService"/>, because there is no
/// operation for this and there should not be: an event store is a log, and nothing in the domain
/// asks for one to be forgotten. This is a sample database being reset, which is a different thing
/// and is why it lives here rather than anywhere the framework can reach.
/// </remarks>
public static class SampleDataEraser
{
    /// <summary>
    /// Deletes everything in the streamed store: snapshots first, then the log they were folded
    /// from.
    /// </summary>
    /// <returns>How many rows went, by table.</returns>
    public static async Task<IReadOnlyList<(string Table, int Rows)>> EraseStreamed(
        StreamedStoreDbContext context, CancellationToken cancellationToken = default) =>
    [
        ("DomainAggregates", await context.Aggregates.ExecuteDeleteAsync(cancellationToken)),
        ("DomainProjections", await context.Projections.ExecuteDeleteAsync(cancellationToken)),
        ("events", await context.Events.ExecuteDeleteAsync(cancellationToken))
    ];

    /// <summary>
    /// Deletes everything in the DCB store.
    /// </summary>
    /// <remarks>
    /// The order is the foreign keys' order: a tag row points at the event it tags, and a tag head
    /// points at the position it is the head of, so both go before the events do.
    /// </remarks>
    /// <returns>How many rows went, by table.</returns>
    public static async Task<IReadOnlyList<(string Table, int Rows)>> EraseDcb(
        DcbStoreDbContext context, CancellationToken cancellationToken = default) =>
    [
        ("DcbSnapshots", await context.DcbSnapshots.ExecuteDeleteAsync(cancellationToken)),
        ("DcbEventTags", await context.DcbEventTags.ExecuteDeleteAsync(cancellationToken)),
        ("DcbTagHeads", await context.DcbTagHeads.ExecuteDeleteAsync(cancellationToken)),
        ("DcbEvents", await context.DcbEvents.ExecuteDeleteAsync(cancellationToken))
    ];
}
