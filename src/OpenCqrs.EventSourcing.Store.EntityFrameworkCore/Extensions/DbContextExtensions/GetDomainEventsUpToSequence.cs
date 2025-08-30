using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves domain events from a specified stream up to and including a specific sequence number,
    /// with optional filtering by event types. This method supports time-travel scenarios, historical
    /// analysis, and point-in-time aggregate reconstruction in event sourcing systems.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event entities and deserialization
    /// capabilities for converting stored events back to domain event objects.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose events should be retrieved.
    /// Events will be filtered to only include those from this specific stream.
    /// </param>
    /// <param name="upToSequence">
    /// The maximum sequence number (inclusive) for events to be included in the results.
    /// Only events with sequence numbers less than or equal to this value will be returned,
    /// enabling point-in-time queries and historical state reconstruction.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the results. When provided, only events
    /// matching the specified types will be included in the returned collection.
    /// If null or empty, all events up to the sequence limit are returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="IDomainEvent"/> objects representing events in the stream
    /// up to the specified sequence number that match the optional filter criteria,
    /// ordered by their sequence numbers. Returns an empty list if no events exist or match the criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get aggregate state at specific point in time
    /// public async Task&lt;OrderAggregate&gt; GetOrderStateAtSequenceAsync(Guid orderId, int targetSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var events = await _context.GetDomainEventsUpToSequence(streamId, targetSequence);
    ///     
    ///     var aggregate = new OrderAggregate();
    ///     aggregate.Apply(events);
    ///     
    ///     _logger.LogInformation("Reconstructed order {OrderId} state at sequence {Sequence} using {EventCount} events",
    ///         orderId, targetSequence, events.Count);
    ///     
    ///     return aggregate;
    /// }
    /// 
    /// // Historical analysis: Compare states at different points
    /// public async Task&lt;OrderComparisonReport&gt; CompareOrderStatesAsync(
    ///     Guid orderId, 
    ///     int earlySequence, 
    ///     int laterSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     
    ///     var earlyEvents = await _context.GetDomainEventsUpToSequence(streamId, earlySequence);
    ///     var laterEvents = await _context.GetDomainEventsUpToSequence(streamId, laterSequence);
    ///     
    ///     var earlyState = new OrderAggregate();
    ///     earlyState.Apply(earlyEvents);
    ///     
    ///     var laterState = new OrderAggregate();
    ///     laterState.Apply(laterEvents);
    ///     
    ///     return new OrderComparisonReport
    ///     {
    ///         OrderId = orderId,
    ///         EarlySequence = earlySequence,
    ///         LaterSequence = laterSequence,
    ///         EarlyState = CreateStateSnapshot(earlyState),
    ///         LaterState = CreateStateSnapshot(laterState),
    ///         Changes = AnalyzeStateChanges(earlyState, laterState),
    ///         EventsBetween = laterEvents.Skip(earlyEvents.Count).ToList()
    ///     };
    /// }
    /// 
    /// // Debugging: Find events leading to specific condition
    /// public async Task&lt;List&lt;IDomainEvent&gt;&gt; FindEventsLeadingToConditionAsync(
    ///     Guid orderId, 
    ///     Func&lt;OrderAggregate, bool&gt; condition)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var maxSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     // Binary search to find the point where condition becomes true
    ///     var (foundSequence, foundEvents) = await FindConditionSequenceAsync(
    ///         streamId, condition, 1, maxSequence);
    ///     
    ///     if (foundSequence &gt; 0)
    ///     {
    ///         _logger.LogInformation("Condition met at sequence {Sequence} for order {OrderId}",
    ///             foundSequence, orderId);
    ///         return foundEvents;
    ///     }
    ///     
    ///     return new List&lt;IDomainEvent&gt;();
    /// }
    /// 
    /// private async Task&lt;(int sequence, List&lt;IDomainEvent&gt; events)&gt; FindConditionSequenceAsync(
    ///     IStreamId streamId, 
    ///     Func&lt;OrderAggregate, bool&gt; condition, 
    ///     int minSequence, 
    ///     int maxSequence)
    /// {
    ///     if (minSequence &gt; maxSequence) return (0, new List&lt;IDomainEvent&gt;());
    ///     
    ///     var midSequence = (minSequence + maxSequence) / 2;
    ///     var events = await _context.GetDomainEventsUpToSequence(streamId, midSequence);
    ///     
    ///     var aggregate = new OrderAggregate();
    ///     aggregate.Apply(events);
    ///     
    ///     if (condition(aggregate))
    ///     {
    ///         // Condition is true, look for earlier occurrence
    ///         var (earlierSequence, earlierEvents) = await FindConditionSequenceAsync(
    ///             streamId, condition, minSequence, midSequence - 1);
    ///         return earlierSequence &gt; 0 ? (earlierSequence, earlierEvents) : (midSequence, events);
    ///     }
    ///     else
    ///     {
    ///         // Condition is false, look later
    ///         return await FindConditionSequenceAsync(streamId, condition, midSequence + 1, maxSequence);
    ///     }
    /// }
    /// 
    /// // Compliance: Generate state report at regulatory checkpoint
    /// public async Task&lt;ComplianceSnapshot&gt; GenerateComplianceSnapshotAsync(
    ///     Guid orderId, 
    ///     DateTime checkpointDate)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     
    ///     // Find the last sequence before or at the checkpoint date
    ///     var allEvents = await _context.GetDomainEvents(streamId);
    ///     var checkpointSequence = allEvents
    ///         .Where(e =&gt; e.OccurredAt &lt;= checkpointDate)
    ///         .LastOrDefault()?.Sequence ?? 0;
    ///     
    ///     if (checkpointSequence == 0)
    ///     {
    ///         return new ComplianceSnapshot
    ///         {
    ///             OrderId = orderId,
    ///             CheckpointDate = checkpointDate,
    ///             Status = "Not Created",
    ///             Message = "Order did not exist at checkpoint date"
    ///         };
    ///     }
    ///     
    ///     var eventsAtCheckpoint = await _context.GetDomainEventsUpToSequence(streamId, checkpointSequence);
    ///     var aggregate = new OrderAggregate();
    ///     aggregate.Apply(eventsAtCheckpoint);
    ///     
    ///     return new ComplianceSnapshot
    ///     {
    ///         OrderId = orderId,
    ///         CheckpointDate = checkpointDate,
    ///         CheckpointSequence = checkpointSequence,
    ///         Status = aggregate.Status.ToString(),
    ///         TotalAmount = aggregate.TotalAmount,
    ///         ItemCount = aggregate.Items.Count,
    ///         EventCount = eventsAtCheckpoint.Count,
    ///         ComplianceFlags = ValidateComplianceRules(aggregate)
    ///     };
    /// }
    /// 
    /// // Testing: Verify aggregate behavior with incremental event application
    /// public async Task&lt;List&lt;AggregateStateTransition&gt;&gt; TraceAggregateEvolutionAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var maxSequence = await _context.GetLatestEventSequence(streamId);
    ///     var transitions = new List&lt;AggregateStateTransition&gt;();
    ///     
    ///     for (int sequence = 1; sequence &lt;= maxSequence; sequence++)
    ///     {
    ///         var events = await _context.GetDomainEventsUpToSequence(streamId, sequence);
    ///         var aggregate = new OrderAggregate();
    ///         aggregate.Apply(events);
    ///         
    ///         var lastEvent = events.LastOrDefault();
    ///         transitions.Add(new AggregateStateTransition
    ///         {
    ///             Sequence = sequence,
    ///             EventType = lastEvent?.GetType().Name,
    ///             EventData = lastEvent,
    ///             AggregateVersion = aggregate.Version,
    ///             AggregateStatus = aggregate.Status,
    ///             StateSnapshot = CreateStateSnapshot(aggregate)
    ///         });
    ///     }
    ///     
    ///     return transitions;
    /// }
    /// 
    /// // Performance: Batch processing with sequence chunking
    /// public async Task ProcessHistoricalDataAsync(
    ///     IStreamId streamId, 
    ///     Func&lt;List&lt;IDomainEvent&gt;, Task&gt; processor,
    ///     int batchSize = 1000)
    /// {
    ///     var maxSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     for (int startSequence = 1; startSequence &lt;= maxSequence; startSequence += batchSize)
    ///     {
    ///         var endSequence = Math.Min(startSequence + batchSize - 1, maxSequence);
    ///         
    ///         var events = await _context.GetDomainEventsUpToSequence(streamId, endSequence);
    ///         var batchEvents = events.Where(e =&gt; e.Sequence &gt;= startSequence).ToList();
    ///         
    ///         await processor(batchEvents);
    ///         
    ///         _logger.LogDebug("Processed events {Start}-{End} of {Total}",
    ///             startSequence, endSequence, maxSequence);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<List<IDomainEvent>> GetDomainEventsUpToSequence(this IDomainDbContext domainDbContext, IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
