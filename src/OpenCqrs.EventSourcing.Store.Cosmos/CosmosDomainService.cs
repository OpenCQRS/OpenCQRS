using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDomainService(ICosmosClientConnection cosmosClientConnection) : IDomainService
{
    private readonly CosmosClient _cosmosClient = new(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);

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

    public Task<int> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
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
