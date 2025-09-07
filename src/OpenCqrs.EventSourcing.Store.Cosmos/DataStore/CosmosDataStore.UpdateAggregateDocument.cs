using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

public partial class CosmosDataStore
{
    /// <summary>
    /// Updates an aggregate document by applying new events and storing the updated state in Cosmos DB.
    /// This method retrieves new events since the aggregate's last update, applies them to the aggregate, 
    /// and creates aggregate event documents to track the relationship between the aggregate and events.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to update.</typeparam>
    /// <param name="streamId">The stream identifier containing the aggregate.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="aggregateDocument">The current aggregate document to update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A result containing the updated aggregate, or a failure if an error occurred.</returns>
    /// <exception cref="Exception">Thrown when the aggregate type does not have an AggregateType attribute.</exception>
    public async Task<Result<TAggregate>> UpdateAggregateDocument<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, AggregateDocument aggregateDocument, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = aggregateDocument.ToAggregate<TAggregate>();

        var currentAggregateVersion = aggregate.Version;

        var newEventDocumentsResult = await GetEventDocumentsFromSequence(streamId, fromSequence: aggregate.LatestEventSequence + 1, aggregate.EventTypeFilter, cancellationToken);
        if (newEventDocumentsResult.IsNotSuccess)
        {
            return newEventDocumentsResult.Failure!;
        }
        var newEventDocuments = newEventDocumentsResult.Value!;
        if (newEventDocuments.Count == 0)
        {
            return aggregate;
        }

        var newDomainEvents = newEventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
        aggregate.Apply(newDomainEvents);
        if (aggregate.Version == currentAggregateVersion)
        {
            return aggregate;
        }

        var newLatestEventSequenceForAggregate = newEventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            var aggregateDocumentToUpdate = aggregate.ToAggregateDocument(streamId, aggregateId, newLatestEventSequenceForAggregate);
            aggregateDocumentToUpdate.CreatedDate = aggregateDocument.CreatedDate;
            aggregateDocumentToUpdate.CreatedBy = aggregateDocument.CreatedBy;
            aggregateDocumentToUpdate.UpdatedDate = timeStamp;
            aggregateDocumentToUpdate.UpdatedBy = currentUserNameIdentifier;
            batch.UpsertItem(aggregateDocumentToUpdate);

            foreach (var eventDocument in newEventDocuments)
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
            ex.AddException(streamId, operationDescription: "Update Aggregate Document");
            return ErrorHandling.DefaultFailure;
        }
    }
}
