using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

/// <summary>
/// Provides extension methods for adding diagnostic information to activities.
/// </summary>
public static class DiagnosticsExtensions
{
    /// <summary>
    /// Adds an activity event for a CosmosDB batch response with aggregate information.
    /// </summary>
    /// <param name="batchResponse">The transactional batch response from CosmosDB.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    public static void AddActivityEvent<TAggregate>(this TransactionalBatchResponse batchResponse, IStreamId streamId, IAggregateId<TAggregate> aggregateId) where TAggregate : IAggregate
    {
        Activity.Current?.AddEvent(new ActivityEvent("CosmosDB Batch Execute", default, new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "aggregateId", aggregateId.ToStoreId() },
            { "cosmosdb.activityId", batchResponse.ActivityId },
            { "cosmosdb.statusCode", batchResponse.StatusCode },
            { "cosmosdb.errorMessage", batchResponse.ErrorMessage },
            { "cosmosdb.requestCharge", batchResponse.RequestCharge },
            { "cosmosdb.Count", batchResponse.Count }
        }));
    }

    /// <summary>
    /// Adds an activity event for a CosmosDB batch response with event document information.
    /// </summary>
    /// <param name="batchResponse">The transactional batch response from CosmosDB.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventDocuments">The collection of event documents processed in the batch.</param>
    public static void AddActivityEvent(this TransactionalBatchResponse batchResponse, IStreamId streamId, IEnumerable<EventDocument> eventDocuments)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "CosmosDB Batch", timestamp: default, new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "eventDocumentIds", string.Join(", ", eventDocuments.Select(document => document.Id))},
            { "cosmosdb.activityId", batchResponse.ActivityId },
            { "cosmosdb.statusCode", batchResponse.StatusCode },
            { "cosmosdb.errorMessage", batchResponse.ErrorMessage },
            { "cosmosdb.requestCharge", batchResponse.RequestCharge },
            { "cosmosdb.Count", batchResponse.Count }
        }));
    }

    public static void AddActivityEvent<T>(this FeedResponse<T> feedResponse, IStreamId streamId, string operationDescription)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "CosmosDB Iterator", timestamp: default, new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "operation", operationDescription },
            { "cosmosdb.activityId", feedResponse.ActivityId },
            { "cosmosdb.statusCode", feedResponse.StatusCode },
            { "cosmosdb.requestCharge", feedResponse.RequestCharge },
            { "cosmosdb.Count", feedResponse.Count }
        }));
    }
    
    /// <summary>
    /// Adds an activity event for concurrency exceptions with sequence information.
    /// </summary>
    /// <param name="streamId">The stream identifier where the concurrency exception occurred.</param>
    /// <param name="expectedEventSequence">The expected event sequence number.</param>
    /// <param name="latestEventSequence">The actual latest event sequence number.</param>
    public static void AddActivityEvent(IStreamId streamId, int expectedEventSequence, int latestEventSequence)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "Concurrency exception", timestamp: default, tags: new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "expectedEventSequence", expectedEventSequence },
            { "latestEventSequence", latestEventSequence }
        }));
    }

    /// <summary>
    /// Adds exception information to the current activity with stream and operation context.
    /// </summary>
    /// <param name="exception">The exception to add to the activity.</param>
    /// <param name="streamId">The stream identifier associated with the operation.</param>
    /// <param name="operationDescription">A description of the operation that caused the exception.</param>
    public static void AddException(this Exception exception, IStreamId streamId, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "streamId", streamId.Id },
            { "operation", operationDescription }
        });
    }
}
