namespace Memoria.Web.Extensibility;

/// <summary>
/// The streamed store, as the pages that read it need it.
/// </summary>
/// <remarks>
/// The whole of what this application asks of the streamed store: three pages, two questions. It
/// is an interface rather than a context because the store is not always a relational one — the
/// same two questions are answered against Cosmos DB by querying documents rather than by
/// composing <c>IQueryable</c>, and neither page should know which it is talking to.
/// <para>
/// Deliberately not a repository over the store's entities. What the pages want is a page of rows
/// already narrowed, ordered and counted, which is a question a database can answer in one trip;
/// anything finer would be answered here by reading more than a page and throwing most of it away.
/// </para>
/// </remarks>
public interface IStreamedReads
{
    /// <summary>
    /// Reads one page of the log.
    /// </summary>
    /// <param name="filter">What to narrow to, in what order, and which page of it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<StoredStreamEvents> Events(
        StreamedEventFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of the snapshots, of either kind of model.
    /// </summary>
    /// <param name="filter">Which kind, what to narrow to, in what order, and which page of it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<StoredStreamSnapshots> Snapshots(
        StreamedSnapshotFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// What one page of the log is narrowed to.
/// </summary>
/// <param name="StreamPattern">
/// The pattern the ids of one stream type match, or null for every stream.
/// </param>
/// <param name="EventType">The binding key to narrow to, or null for every type.</param>
/// <param name="Text">
/// Text the row must carry, in its stream, its key or its payload, or null to keep them all.
/// </param>
/// <param name="Descending">Whether the newest come first.</param>
/// <param name="Page">The page asked for, from one.</param>
/// <param name="Size">The rows per page.</param>
/// <remarks>
/// A record rather than the arguments themselves, because the two reads take six and nine of them:
/// at that width a caller passing one in the wrong place still compiles, and a reader has to count
/// commas to see which null means which. It also means a filter added later reaches both
/// implementations without changing either signature.
/// </remarks>
public sealed record StreamedEventFilter(
    string? StreamPattern,
    string? EventType,
    string? Text,
    bool Descending,
    int Page,
    int Size);

/// <summary>
/// What one page of the snapshots is narrowed to.
/// </summary>
/// <param name="Kind">Which of the two models to read.</param>
/// <param name="StreamPattern">
/// The pattern the ids of one stream type match, or null for every stream.
/// </param>
/// <param name="ModelType">The binding key to narrow to, or null for every type.</param>
/// <param name="IdentifierPattern">
/// The pattern the ids of one identifier match, or null for every identifier.
/// </param>
/// <param name="Text">
/// Text the row must carry, in its stream id or its own id, or null for any row.
/// </param>
/// <param name="Sort">Which of the two dates to order by.</param>
/// <param name="Descending">Whether the newest come first.</param>
/// <param name="Page">The page asked for, from one.</param>
/// <param name="Size">The rows per page.</param>
public sealed record StreamedSnapshotFilter(
    StreamedModelKind Kind,
    string? StreamPattern,
    string? ModelType,
    string? IdentifierPattern,
    string? Text,
    InstanceSort Sort,
    bool Descending,
    int Page,
    int Size);
