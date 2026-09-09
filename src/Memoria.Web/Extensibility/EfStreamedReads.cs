using Memoria.Web.Data;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Answers the pages' two questions from a relational store, through Entity Framework Core.
/// </summary>
/// <param name="context">The streamed store's three tables.</param>
/// <remarks>
/// The queries themselves stay in <see cref="StreamedEvents"/> and <see cref="StreamedSnapshots"/>,
/// which is where their reasoning is written down. This unpacks a filter into the arguments they
/// already take and does nothing else: a store that answers by composing <c>IQueryable</c> needs no
/// second account of what the pages are asking for.
/// </remarks>
public sealed class EfStreamedReads(StreamedStoreDbContext context) : IStreamedReads
{
    /// <inheritdoc />
    public Task<StoredStreamEvents> Events(
        StreamedEventFilter filter, CancellationToken cancellationToken = default) =>
        StreamedEvents.Page(
            context,
            filter.StreamPattern,
            filter.EventType,
            filter.Text,
            filter.Descending,
            filter.Page,
            filter.Size,
            cancellationToken);

    /// <inheritdoc />
    public Task<StoredStreamSnapshots> Snapshots(
        StreamedSnapshotFilter filter, CancellationToken cancellationToken = default) =>
        StreamedSnapshots.Page(
            context,
            filter.Kind,
            filter.StreamPattern,
            filter.ModelType,
            filter.IdentifierPattern,
            filter.Text,
            filter.Sort,
            filter.Descending,
            filter.Page,
            filter.Size,
            cancellationToken);
}
