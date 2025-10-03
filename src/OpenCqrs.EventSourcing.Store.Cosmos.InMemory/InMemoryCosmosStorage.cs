
using System.Collections.Concurrent;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.InMemory;

/// <summary>
/// Shared in-memory storage for Cosmos DB implementation.
/// This class provides thread-safe storage for aggregates, events, and aggregate-event relationships.
/// </summary>
public class InMemoryCosmosStorage
{
    public ConcurrentDictionary<string, AggregateDocument> AggregateDocuments { get; } = new();
    public ConcurrentDictionary<string, EventDocument> EventDocuments { get; } = new();
    public ConcurrentDictionary<string, ConcurrentBag<AggregateEventDocument>> AggregateEventDocuments { get; } = new();
    public ConcurrentDictionary<string, int> StreamSequences { get; } = new();

    /// <summary>
    /// Clears all stored data. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        AggregateDocuments.Clear();
        EventDocuments.Clear();
        AggregateEventDocuments.Clear();
        StreamSequences.Clear();
    }
}
