using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    // TODO: GetEventEntitiesBetweenSequences (Issue #124)
    // TODO: GetEventEntitiesUpToDate (Issue #124)
    // TODO: GetEventEntitiesFromDate (Issue #124)
    // TODO: GetEventEntitiesBetweenDates (Issue #124)

    // TODO: GetEventEntities as stream (Issue #122)
    // TODO: GetEventEntitiesUpToSequence as stream (Issue #122)
    // TODO: GetEventEntitiesFromSequence as stream (Issue #122)
    // TODO: GetEventEntitiesBetweenSequences as stream (Issue #122)
    // TODO: GetEventEntitiesUpToDate as stream (Issue #122)
    // TODO: GetEventEntitiesFromDate as stream (Issue #122)
    // TODO: GetEventEntitiesBetweenDates as stream (Issue #122)

    private static async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var currentAggregateVersion = aggregate.Version;

        var newEventEntities = await domainDbContext.GetEventEntitiesFromSequence(streamId, fromSequence: aggregate.LatestEventSequence + 1, aggregate.EventTypeFilter, cancellationToken);
        if (newEventEntities.Count == 0)
        {
            return aggregate;
        }

        var newDomainEvents = newEventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
        aggregate.Apply(newDomainEvents);

        if (aggregate.Version == currentAggregateVersion)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = newEventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        var trackedAggregateEntity = domainDbContext.TrackAggregateEntity(streamId, aggregateId, aggregate, latestEventSequenceForAggregate, aggregateIsNew: false);
        domainDbContext.TrackAggregateEventEntities(trackedAggregateEntity, newEventEntities);

        try
        {
            await domainDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Update Aggregate");
            return ErrorHandling.DefaultFailure;
        }

        domainDbContext.DetachAggregate(aggregateId, aggregate);

        return aggregate;
    }

    private static List<EventEntity> TrackEventEntities(this IDomainDbContext domainDbContext, IStreamId streamId, IDomainEvent[] domainEvents, int startingEventSequence)
    {
        var eventEntities = domainEvents.Select((domainEvent, i) => domainEvent.ToEventEntity(streamId, sequence: startingEventSequence + i)).ToList();
        domainDbContext.Events.AddRange(eventEntities);
        return eventEntities;
    }

    private static AggregateEntity TrackAggregateEntity(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId aggregateId, IAggregate aggregate, int newLatestEventSequence, bool aggregateIsNew)
    {
        var aggregateEntity = aggregate.ToAggregateEntity(streamId, aggregateId, newLatestEventSequence);
        if (!aggregateIsNew)
        {
            domainDbContext.Aggregates.Update(aggregateEntity);
        }
        else
        {
            domainDbContext.Aggregates.Add(aggregateEntity);
        }
        return aggregateEntity;
    }

    private static List<AggregateEventEntity> TrackAggregateEventEntities(this IDomainDbContext domainDbContext, AggregateEntity aggregateEntity, List<EventEntity> eventEntities)
    {
        var aggregateEventEntities = eventEntities.Select(eventEntity => new AggregateEventEntity { AggregateId = aggregateEntity.Id, EventId = eventEntity.Id }).ToList();
        domainDbContext.AggregateEvents.AddRange(aggregateEventEntities);
        return aggregateEventEntities;
    }
}
