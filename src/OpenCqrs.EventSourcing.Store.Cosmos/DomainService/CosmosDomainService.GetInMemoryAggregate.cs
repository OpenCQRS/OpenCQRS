using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets an aggregate built-in-memory from events, optionally up to a specific sequence number.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to build.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="upToSequence">Optional sequence number to build up to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the in-memory aggregate or failure information.</returns>
    public async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventDocumentsResult = upToSequence > 0
            ? await _cosmosDataStore.GetEventDocumentsUpToSequence(streamId, upToSequence.Value, aggregate.EventTypeFilter, cancellationToken)
            : await _cosmosDataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);

        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventDocuments.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }
}
