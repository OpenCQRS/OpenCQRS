using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

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
    
    public async Task<List<EventDocument>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
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
        
        using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            eventDocuments.AddRange(response);
        }

        return eventDocuments;
    }
}
