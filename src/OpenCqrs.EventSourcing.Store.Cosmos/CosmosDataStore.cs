using System.Diagnostics;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDataStore : ICosmosDataStore
{
    private readonly TimeProvider _timeProvider;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public CosmosDataStore(ICosmosClientConnection cosmosClientConnection, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _cosmosClient = new CosmosClient(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
        _container = _cosmosClient.GetContainer(cosmosClientConnection.DatabaseName, cosmosClientConnection.ContainerName);
    }

    public async Task<Result<AggregateDocument?>> GetAggregateDocument<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            return new Failure
            (
                Title: "Aggregate type not found",
                Description: $"Aggregate {typeof(TAggregate).Name} does not have an AggregateType attribute."
            );
        }

        var aggregateDocumentId = aggregateId.ToIdWithTypeVersion(aggregateType.Version);

        try
        {
            var response = await _container.ReadItemAsync<AggregateDocument>(aggregateDocumentId, new PartitionKey(streamId.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (AggregateDocument?)null;
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the aggregate document", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the aggregate document",
                Description: "There was an error when retrieving the aggregate document"
            );
        }
    }

    public async Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;

        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }

        var eventDocuments = new List<EventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                eventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the event documents", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the event documents",
                Description: "There was an error when retrieving the event documents"
            );
        }

        return eventDocuments;
    }
}
