using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class DiagnosticsExtensions
{
    public static void AddActivityEvent(this TransactionalBatchResponse batchResponse, IStreamId streamId, IAggregateId aggregateId, AggregateType aggregateType)
    {
        Activity.Current?.AddEvent(new ActivityEvent("CosmosDB Batch Execute", default, new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "aggregateId", aggregateId.ToIdWithTypeVersion(aggregateType.Version) },
            { "cosmosdb.activityId", batchResponse.ActivityId },
            { "cosmosdb.statusCode", batchResponse.StatusCode },
            { "cosmosdb.errorMessage", batchResponse.ErrorMessage },
            { "cosmosdb.requestCharge", batchResponse.RequestCharge },
            { "cosmosdb.Count", batchResponse.Count }
        }));
    }

    public static void AddActivityEvent(this TransactionalBatchResponse batchResponse, IStreamId streamId, IEnumerable<EventDocument> eventDocuments)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "CosmosDB Batch Execute", timestamp: default, new ActivityTagsCollection
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

    public static void AddActivityEvent(IStreamId streamId, int expectedEventSequence, int latestEventSequence)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "Concurrency exception", timestamp: default, tags: new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "expectedEventSequence", expectedEventSequence },
            { "latestEventSequence", latestEventSequence }
        }));
    }

    public static void AddException(this Exception exception, IStreamId streamId, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "streamId", streamId.Id },
            { "operation", operationDescription }
        });
    }
}
