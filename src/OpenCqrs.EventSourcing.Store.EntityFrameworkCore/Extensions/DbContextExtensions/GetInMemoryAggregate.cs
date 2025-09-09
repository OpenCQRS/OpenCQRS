using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    public static async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntities(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public static async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, int upToSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntitiesUpToSequence(streamId, upToSequence, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public static async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, DateTimeOffset upToDate, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntitiesUpToDate(streamId, upToDate, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }
}
