using System.Diagnostics;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

/// <summary>
/// Provides extension methods for <see cref="IDomainDbContext"/> that implement core event sourcing operations
/// including aggregate persistence, event tracking, domain event management, and aggregate reconstruction.
/// These methods enable seamless integration with Entity Framework Core for event sourcing scenarios.
/// </summary>
/// <example>
/// <code>
/// // Example usage in a command handler
/// public class CreateOrderCommandHandler : IRequestHandler&lt;CreateOrderCommand&gt;
/// {
///     private readonly IDomainDbContext _context;
///     
///     public CreateOrderCommandHandler(IDomainDbContext context)
///     {
///         _context = context;
///     }
///     
///     public async Task&lt;Result&gt; Handle(CreateOrderCommand request, CancellationToken cancellationToken)
///     {
///         var streamId = new StreamId($"order-{request.OrderId}");
///         var aggregateId = new OrderAggregateId(request.OrderId);
///         
///         // Create new aggregate
///         var order = new OrderAggregate(request.OrderId, request.CustomerId);
///         order.AddItem(request.ProductId, request.Quantity);
///         
///         // Save with concurrency control
///         return await _context.Save(streamId, aggregateId, order, expectedEventSequence: 0, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public static partial class IDomainDbContextExtensions
{
    // TODO: GetDomainEventsBetweenSequences (Issue #124)
    // TODO: GetDomainEventsUpToDate (Issue #124)
    // TODO: GetDomainEventsFromDate (Issue #124)
    // TODO: GetDomainEventsBetweenDates (Issue #124)

    // TODO: GetDomainEvents as stream (Issue #122)
    // TODO: GetDomainEventsUpToSequence as stream (Issue #122)
    // TODO: GetDomainEventsFromSequence as stream (Issue #122)
    // TODO: GetDomainEventsBetweenSequences as stream (Issue #122)
    // TODO: GetDomainEventsUpToDate as stream (Issue #122)
    // TODO: GetDomainEventsFromDate as stream (Issue #122)
    // TODO: GetDomainEventsBetweenDates as stream (Issue #122)

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
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when updating the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving changes",
                Description: "There was an error when updating the aggregate"
            );
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
