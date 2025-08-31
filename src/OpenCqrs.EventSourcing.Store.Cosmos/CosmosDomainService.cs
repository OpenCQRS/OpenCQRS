using System.Reflection;
using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDomainService : IDomainService
{
    private readonly TimeProvider _timeProvider;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    
    public CosmosDomainService(ICosmosClientConnection cosmosClientConnection, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
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
            const string sql = "SELECT VALUE COUNT(1) FROM c WHERE c.streamId = @streamId AND c.type = @documentType";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();
            
            const string sql = "SELECT VALUE COUNT(1) FROM c WHERE c.streamId = @streamId AND c.type = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType)";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event)
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

        try
        {
            var latestEventSequence = await GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
            if (latestEventSequence != expectedEventSequence)
            {
                return new Failure
                (
                    Title: "Concurrency exception",
                    Description: $"Expected event sequence {expectedEventSequence} but found {latestEventSequence}"
                );
            }
        
            var aggregateTypeAttribute = aggregate.GetType().GetCustomAttribute<AggregateType>();
            if (aggregateTypeAttribute == null)
            {
                throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
            }
            
            var newLatestEventSequenceForAggregate = latestEventSequence + aggregate.UncommittedEvents.Count();
            var timeStamp = _timeProvider.GetUtcNow();
            
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            foreach (var @event in aggregate.UncommittedEvents)
            {
                var eventDocument = @event.ToEventDocument(streamId, sequence: ++latestEventSequence, timeStamp);
                batch.CreateItem(eventDocument);

                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version)}:{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                batch.CreateItem(aggregateEventDocument);
            }
            
            batch.UpsertItem(aggregate.ToAggregateDocument(streamId, aggregateId, newLatestEventSequenceForAggregate, timeStamp));
            
            var transactionalBatchResponse = await batch.ExecuteAsync(cancellationToken);
            return !transactionalBatchResponse.IsSuccessStatusCode 
                ? new Failure(Title: "Cosmos batch failed", Description: transactionalBatchResponse.ErrorMessage) 
                : Result.Ok();
        }
        catch (Exception e)
        {
            // TODO: Handle failure
            throw;
        }
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
