namespace OpenCqrs.EventSourcing;

/// <summary>
/// Represents the mode of reading aggregates.
/// </summary>
public enum ReadMode
{
    /// <summary>
    /// Specifies that the latest snapshot should be used when getting
    /// an aggregate, without including any subsequent new events.
    /// </summary>
    LatestSnapshot,

    /// <summary>
    /// Specifies that both the latest snapshot and any subsequent new events
    /// should be used when getting an aggregate.
    /// </summary>
    LatestSnapshotPlusNewEvents,

    /// <summary>
    /// Specifies that the system should use the latest snapshot if available
    /// or create a new snapshot if no existing snapshot is found.
    /// </summary>
    LatestSnapshotOrCreateNew
}
