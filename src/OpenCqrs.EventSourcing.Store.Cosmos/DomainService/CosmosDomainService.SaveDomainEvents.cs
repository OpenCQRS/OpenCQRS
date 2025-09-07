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
    /// Saves domain events to the event store.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="domainEvents">The domain events to save.</param>
    /// <param name="expectedEventSequence">The expected current event sequence for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        if (domainEvents.Length == 0)
        {
            return Result.Ok();
        }

        var latestEventSequenceResult = await GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
        if (latestEventSequenceResult.IsNotSuccess)
        {
            return latestEventSequenceResult.Failure!;
        }
        var latestEventSequence = latestEventSequenceResult.Value;
        if (latestEventSequence != expectedEventSequence)
        {
            DiagnosticsExtensions.AddActivityEvent(streamId, expectedEventSequence, latestEventSequence);
            return ErrorHandling.DefaultFailure;
        }

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));
            var eventDocuments = new List<EventDocument>();
            foreach (var @event in domainEvents)
            {
                var eventDocument = @event.ToEventDocument(streamId, sequence: ++latestEventSequence);
                eventDocument.CreatedDate = timeStamp;
                eventDocument.CreatedBy = currentUserNameIdentifier;
                eventDocuments.Add(eventDocument);
                batch.CreateItem(eventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            batchResponse.AddActivityEvent(streamId, eventDocuments);
            return batchResponse.IsSuccessStatusCode ? Result.Ok() : ErrorHandling.DefaultFailure;
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Save Domain Events");
            return ErrorHandling.DefaultFailure;
        }
    }
}
