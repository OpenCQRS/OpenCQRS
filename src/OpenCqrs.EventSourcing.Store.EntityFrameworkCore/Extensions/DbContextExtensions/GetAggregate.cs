﻿using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves an aggregate from the event store, reconstructing it from events if no snapshot exists.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate to retrieve.</typeparam>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="aggregateId">The unique identifier for the aggregate instance.</param>
    /// <param name="applyNewDomainEvents">Whether to apply new events after the snapshot.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the aggregate or a failure.</returns>
    /// <example>
    /// <code>
    /// var result = await context.GetAggregate(streamId, aggregateId);
    /// if (!result.IsSuccess)
    /// {
    ///     return result.Failure;
    /// }
    /// var aggregate = result.Value;
    /// </code>
    /// </example>
    public static async Task<Result<TAggregate>> GetAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregateRoot, new()
    {
        var aggregateEntity = await domainDbContext.Aggregates.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == aggregateId.ToStoreId(), cancellationToken);
        if (aggregateEntity is not null)
        {
            var currentAggregate = aggregateEntity.ToAggregate<TAggregate>();
            if (!applyNewDomainEvents)
            {
                return currentAggregate;
            }
            return await domainDbContext.UpdateAggregate(streamId, aggregateId, currentAggregate, cancellationToken);
        }

        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntities(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        var domainEvents = eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
        aggregate.Apply(domainEvents);

        if (aggregate.Version == 0)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        var trackedAggregateEntity = domainDbContext.TrackAggregateEntity(streamId, aggregateId, aggregate, latestEventSequenceForAggregate, aggregateIsNew: true);
        domainDbContext.TrackAggregateEventEntities(trackedAggregateEntity, eventEntities);

        try
        {
            await domainDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operation: "Get Aggregate");
            return ErrorHandling.DefaultFailure;
        }

        domainDbContext.DetachAggregate(aggregateId, aggregate);

        return aggregate;
    }
}
