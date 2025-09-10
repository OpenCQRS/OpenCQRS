﻿using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Tracks an aggregate's state changes based on a list of event entities.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="aggregateId">The unique identifier for the aggregate.</param>
    /// <param name="eventEntities">A list of event entities to apply.</param>
    /// <param name="expectedEventSequence">The expected sequence number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the tracked aggregate and relationship entities or a failure.</returns>
    /// <example>
    /// <code>
    /// var result = await context.TrackEventEntities(streamId, aggregateId, eventEntities, expectedSequence);
    /// if (!result.IsSuccess)
    /// {
    ///     return result.Failure;
    /// }
    /// var (aggregateEntity, aggregateEventEntities) = result.Value;
    /// </code>
    /// </example>
    public static async Task<Result<(AggregateEntity? AggregateEntity, List<AggregateEventEntity>? AggregateEventEntities)>> TrackEventEntities<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, List<EventEntity> eventEntities, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        if (eventEntities.Count == 0)
        {
            return (null, null);
        }

        var aggregateResult = await domainDbContext.GetAggregate(streamId, aggregateId, cancellationToken: cancellationToken);
        if (aggregateResult.IsNotSuccess)
        {
            return aggregateResult.Failure!;
        }

        var aggregate = aggregateResult.Value!;
        var aggregateIsNew = aggregate.Version == 0;

        var eventEntitiesHandledByTheAggregate = new Dictionary<int, EventEntity>();
        for (var i = 0; i < eventEntities.Count; i++)
        {
            var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(eventEntities[i].EventType, out var eventType);
            if (typeFound is false)
            {
                throw new InvalidOperationException($"Event type {eventEntities[i].EventType} not found in TypeBindings");
            }

            if (aggregate.IsDomainEventHandled(eventType!))
            {
                eventEntitiesHandledByTheAggregate.Add(i + 1, eventEntities[i]);
            }
        }

        if (eventEntitiesHandledByTheAggregate.Count == 0)
        {
            return (null, null);
        }

        aggregate.Apply(eventEntitiesHandledByTheAggregate.Select(domainEvent => domainEvent.Value.ToDomainEvent()));

        var newLatestEventSequenceForAggregate = expectedEventSequence + eventEntitiesHandledByTheAggregate.Last().Key;
        var trackedAggregateEntity = domainDbContext.TrackAggregateEntity(streamId, aggregateId, aggregate, newLatestEventSequenceForAggregate, aggregateIsNew);
        var trackedAggregateEventEntities = domainDbContext.TrackAggregateEventEntities(trackedAggregateEntity, eventEntitiesHandledByTheAggregate.Select(e => e.Value).ToList());

        return (trackedAggregateEntity, trackedAggregateEventEntities);
    }
}
