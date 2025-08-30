using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore;

public class EntityFrameworkCoreDomainService(IDomainDbContext domainDbContext) : IDomainService
{
    public async Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetAggregate(streamId, aggregateId, applyNewDomainEvents, cancellationToken);
    }

    public async Task<List<IDomainEvent>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEvents(streamId, eventTypeFilter, cancellationToken);
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetDomainEventsAppliedToAggregate(aggregateId, cancellationToken);
    }

    public async Task<List<IDomainEvent>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsFromSequence(streamId, fromSequence, eventTypeFilter, cancellationToken);
    }

    public async Task<List<IDomainEvent>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
    }

    public async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetInMemoryAggregate(streamId, aggregateId, upToSequence, cancellationToken);
    }

    public async Task<int> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetLatestEventSequence(streamId, eventTypeFilter, cancellationToken);
    }

    public async Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
        return await domainDbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence, cancellationToken);
    }

    public async Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.SaveDomainEvents(streamId, domainEvents, expectedEventSequence, cancellationToken);
    }

    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.UpdateAggregate(streamId, aggregateId, cancellationToken);
    }

    public void Dispose() => domainDbContext.Dispose();
}
