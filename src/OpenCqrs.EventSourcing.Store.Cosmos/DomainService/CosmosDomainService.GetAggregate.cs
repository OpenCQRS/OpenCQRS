using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets an aggregate from the event store and optionally applies new domain events.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="applyNewDomainEvents">Whether to apply new domain events to update the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the aggregate or failure information.</returns>
    public async Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateDocumentResult = await _cosmosDataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
        if (aggregateDocumentResult.IsNotSuccess)
        {
            return aggregateDocumentResult.Failure!;
        }

        if (aggregateDocumentResult.Value != null)
        {
            var currentAggregate = aggregateDocumentResult.Value.ToAggregate<TAggregate>();
            if (!applyNewDomainEvents)
            {
                return currentAggregate;
            }
            return await UpdateAggregate(streamId, aggregateId, cancellationToken);
        }

        var aggregate = new TAggregate();

        var eventDocumentsResult = await _cosmosDataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        var domainEvents = eventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
        aggregate.Apply(domainEvents);
        if (aggregate.Version == 0)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = eventDocuments.OrderBy(eventDocument => eventDocument.Sequence).Last().Sequence;
        var aggregateDocument = aggregate.ToAggregateDocument(streamId, aggregateId, latestEventSequenceForAggregate);

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            aggregateDocument.CreatedDate = timeStamp;
            aggregateDocument.CreatedBy = currentUserNameIdentifier;
            aggregateDocument.UpdatedDate = timeStamp;
            aggregateDocument.UpdatedBy = currentUserNameIdentifier;
            batch.CreateItem(aggregateDocument);

            foreach (var eventDocument in eventDocuments)
            {
                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToStoreId()}|{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToStoreId(),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                batch.CreateItem(aggregateEventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            batchResponse.AddActivityEvent(streamId, aggregateId);
            return batchResponse.IsSuccessStatusCode ? aggregate : ErrorHandling.DefaultFailure;
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Get Aggregate");
            return ErrorHandling.DefaultFailure;
        }
    }
}
