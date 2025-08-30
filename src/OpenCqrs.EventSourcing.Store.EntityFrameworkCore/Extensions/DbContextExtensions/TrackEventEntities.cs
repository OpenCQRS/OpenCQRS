using OpenCqrs.EventSourcing.Data;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Tracks an aggregate's state changes based on a list of event entities, applying only events
    /// that the aggregate can handle and updating its snapshot accordingly. This method enables
    /// selective event application and aggregate synchronization in complex event sourcing scenarios.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate being tracked. Must implement <see cref="IAggregate"/> and have
    /// a parameterless constructor to support aggregate reconstruction and event application.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to aggregate storage and change tracking
    /// capabilities for managing entity state transitions.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream containing the events to be applied to the aggregate,
    /// ensuring proper stream isolation and event ordering.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate that will receive the event applications
    /// and state updates based on the provided event entities.
    /// </param>
    /// <param name="eventEntities">
    /// A list of event entities to be evaluated and potentially applied to the aggregate. Only events
    /// that the aggregate can handle will be processed and applied to update the aggregate state.
    /// </param>
    /// <param name="expectedEventSequence">
    /// The expected sequence number representing the base position from which new events will be
    /// applied, used for calculating the final aggregate event sequence position.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a tuple with tracked entities:
    /// <list type="bullet">
    /// <item><description><see cref="AggregateEntity"/>?: The updated aggregate snapshot entity, null if no applicable events</description></item>
    /// <item><description><see cref="List{AggregateEventEntity}"/>?: The relationship entities linking aggregate to handled events, null if no events</description></item>
    /// </list>
    /// On failure, returns a <see cref="Failure"/> with aggregate retrieval errors or processing issues.
    /// </returns>
    /// <example>
    /// <code>
    /// // Multi-aggregate event application
    /// public async Task&lt;Result&gt; ApplyEventsToMultipleAggregatesAsync(
    ///     IStreamId streamId, 
    ///     List&lt;EventEntity&gt; newEvents)
    /// {
    ///     var orderAggregateId = new OrderAggregateId(Guid.Parse("order-123"));
    ///     var customerAggregateId = new CustomerAggregateId(Guid.Parse("customer-456"));
    ///     var expectedSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     // Apply events to order aggregate
    ///     var orderTrackResult = await _context.Track(streamId, orderAggregateId, newEvents, expectedSequence);
    ///     if (orderTrackResult.IsNotSuccess)
    ///         return orderTrackResult.Failure!;
    ///     
    ///     // Apply events to customer aggregate
    ///     var customerTrackResult = await _context.Track(streamId, customerAggregateId, newEvents, expectedSequence);
    ///     if (customerTrackResult.IsNotSuccess)
    ///         return customerTrackResult.Failure!;
    ///     
    ///     return await _context.Save();
    /// }
    /// 
    /// // Event replay with aggregate synchronization
    /// public class EventReplayService
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; ReplayEventsForAggregateAsync&lt;T&gt;(
    ///         IStreamId sourceStreamId,
    ///         IAggregateId&lt;T&gt; aggregateId,
    ///         int fromSequence,
    ///         int toSequence) where T : IAggregate, new()
    ///     {
    ///         // Get events from source stream
    ///         var events = await _context.GetEventEntitiesFromSequence(sourceStreamId, fromSequence);
    ///         var eventsToReplay = events.Where(e =&gt; e.Sequence &lt;= toSequence).ToList();
    ///         
    ///         if (eventsToReplay.Count == 0)
    ///             return Result.Ok();
    ///         
    ///         // Apply events to aggregate
    ///         var trackResult = await _context.Track(sourceStreamId, aggregateId, eventsToReplay, fromSequence - 1);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var (aggregateEntity, relationships) = trackResult.Value!;
    ///         
    ///         // Validate replay results
    ///         if (aggregateEntity == null || relationships == null)
    ///         {
    ///             return new Failure("Replay failed", "No applicable events found for aggregate");
    ///         }
    ///         
    ///         return await _context.Save();
    ///     }
    /// }
    /// 
    /// // Selective event processing for aggregate projections
    /// public class AggregateProjectionHandler : IRequestHandler&lt;UpdateProjectionRequest&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; Handle(UpdateProjectionRequest request, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId(request.StreamId);
    ///         var aggregateId = new ProjectionAggregateId(request.AggregateId);
    ///         
    ///         // Get new events since last projection update
    ///         var newEvents = await GetNewEventsSinceLastUpdate(streamId, request.LastProcessedSequence);
    ///         
    ///         if (newEvents.Count == 0)
    ///             return Result.Ok();
    ///         
    ///         // Apply relevant events to projection aggregate
    ///         var trackResult = await _context.Track(
    ///             streamId, 
    ///             aggregateId, 
    ///             newEvents, 
    ///             request.LastProcessedSequence, 
    ///             cancellationToken);
    ///             
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var (projectionAggregate, relationships) = trackResult.Value!;
    ///         
    ///         // Update projection metadata
    ///         if (projectionAggregate != null)
    ///         {
    ///             projectionAggregate.LastUpdatedAt = DateTime.UtcNow;
    ///             projectionAggregate.ProcessedEventCount = relationships?.Count ?? 0;
    ///         }
    ///         
    ///         return await _context.Save(cancellationToken);
    ///     }
    /// }
    /// 
    /// // Cross-aggregate event impact analysis
    /// public async Task&lt;Result&lt;AggregateImpactReport&gt;&gt; AnalyzeEventImpactAsync(
    ///     IStreamId streamId,
    ///     List&lt;EventEntity&gt; proposedEvents,
    ///     List&lt;IAggregateId&gt; candidateAggregates)
    /// {
    ///     var report = new AggregateImpactReport();
    ///     var baseSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     foreach (var aggregateId in candidateAggregates)
    ///     {
    ///         // Test event application without persistence
    ///         var trackResult = await _context.Track(streamId, aggregateId, proposedEvents, baseSequence);
    ///         
    ///         if (trackResult.IsSuccess)
    ///         {
    ///             var (aggregate, relationships) = trackResult.Value!;
    ///             
    ///             if (relationships?.Count &gt; 0)
    ///             {
    ///                 report.AffectedAggregates.Add(new AggregateImpact
    ///                 {
    ///                     AggregateId = aggregateId,
    ///                     ApplicableEvents = relationships.Count,
    ///                     EstimatedChanges = CalculateExpectedChanges(aggregate)
    ///                 });
    ///             }
    ///         }
    ///     }
    ///     
    ///     // Clear tracking without persisting
    ///     _context.ChangeTracker.Clear();
    ///     
    ///     return report;
    /// }
    /// 
    /// // Event-driven aggregate updates with filtering
    /// public class SmartAggregateUpdater
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&lt;int&gt;&gt; UpdateAggregateWithFilteredEventsAsync&lt;T&gt;(
    ///         IStreamId streamId,
    ///         IAggregateId&lt;T&gt; aggregateId,
    ///         Func&lt;EventEntity, bool&gt; eventFilter) where T : IAggregate, new()
    ///     {
    ///         // Get current aggregate state
    ///         var aggregateResult = await _context.GetAggregate(streamId, aggregateId);
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///         
    ///         var aggregate = aggregateResult.Value!;
    ///         
    ///         // Get new events since aggregate's last sequence
    ///         var newEvents = await _context.GetEventEntitiesFromSequence(streamId, aggregate.LatestEventSequence + 1);
    ///         var filteredEvents = newEvents.Where(eventFilter).ToList();
    ///         
    ///         if (filteredEvents.Count == 0)
    ///             return 0; // No applicable events
    ///         
    ///         // Apply filtered events
    ///         var trackResult = await _context.Track(streamId, aggregateId, filteredEvents, aggregate.LatestEventSequence);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var saveResult = await _context.Save();
    ///         return saveResult.IsSuccess ? filteredEvents.Count : saveResult.Failure!;
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<(AggregateEntity? AggregateEntity, List<AggregateEventEntityForEfCore>? AggregateEventEntities)>> TrackEventEntities<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, List<EventEntity> eventEntities, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
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
            if (aggregate.IsDomainEventHandled(eventEntities[i].ToDomainEventType()))
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
