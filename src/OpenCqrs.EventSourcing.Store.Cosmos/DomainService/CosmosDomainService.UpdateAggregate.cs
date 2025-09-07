using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Updates an aggregate by applying new events since its last snapshot.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to update.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the updated aggregate or failure information.</returns>
    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateDocumentResult = await _cosmosDataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
        if (aggregateDocumentResult.IsNotSuccess)
        {
            return aggregateDocumentResult.Failure!;
        }
        var aggregateDocument = aggregateDocumentResult.Value;
        if (aggregateDocument is null)
        {
            return new TAggregate();
        }

        return await _cosmosDataStore.UpdateAggregateDocument(streamId, aggregateId, aggregateDocument, cancellationToken);
    }
}
