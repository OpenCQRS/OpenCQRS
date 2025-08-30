using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDomainService : IDomainService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    
    public CosmosDomainService(ICosmosClientConnection cosmosClientConnection)
    {
        _cosmosClient = new CosmosClient(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
        _container = _cosmosClient.GetContainer(cosmosClientConnection.DatabaseName, cosmosClientConnection.ContainerName);
    }

    public Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        throw new NotImplementedException();
    }

    public Task<List<IDomainEvent>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        throw new NotImplementedException();
    }

    public Task<List<IDomainEvent>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<IDomainEvent>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        throw new NotImplementedException();
    }

    public async Task<int> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT VALUE COUNT(1) FROM c WHERE c.streamId = @streamId AND c.type = 'event'";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();
            
            const string sql = "SELECT VALUE COUNT(1) FROM c WHERE c.aggregateId = @aggregateId AND c.type = 'event' AND ARRAY_CONTAINS(@eventTypes, c.eventType)";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }
        
        using var iterator = _container.GetItemQueryIterator<int>(queryDefinition);
        var count = 0;
        while (iterator.HasMoreResults)
        {
          var response = await iterator.ReadNextAsync(cancellationToken);
          count += response.FirstOrDefault();
        }

        return count;
    }

    public async Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
        if (!aggregate.UncommittedEvents.Any())
        {
            return Result.Ok();
        }
        
        var latestEventSequence = await GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
        if (latestEventSequence != expectedEventSequence)
        {
            return new Failure
            (
                Title: "Concurrency exception",
                Description: $"Expected event sequence {expectedEventSequence} but found {latestEventSequence}"
            );
        }
        
        throw new NotImplementedException();
    }

    public Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        throw new NotImplementedException();
    }

    public void Dispose() => _cosmosClient.Dispose();
}
