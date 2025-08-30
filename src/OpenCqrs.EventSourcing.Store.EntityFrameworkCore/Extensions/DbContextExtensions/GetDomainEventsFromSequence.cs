using OpenCqrs.EventSourcing.Data;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves domain events from a specified stream starting from a specific sequence number onwards,
    /// with optional filtering by event types. This method supports incremental processing, catch-up scenarios,
    /// and differential analysis in event sourcing systems.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event entities and deserialization
    /// capabilities for converting stored events back to domain event objects.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose events should be retrieved.
    /// Events will be filtered to only include those from this specific stream.
    /// </param>
    /// <param name="fromSequence">
    /// The minimum sequence number (inclusive) for events to be included in the results.
    /// Only events with sequence numbers greater than or equal to this value will be returned,
    /// enabling incremental processing and catch-up scenarios.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the results. When provided, only events
    /// matching the specified types will be included in the returned collection.
    /// If null or empty, all events from the starting sequence onwards are returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="IDomainEvent"/> objects representing events in the stream
    /// starting from the specified sequence number that match the optional filter criteria,
    /// ordered by their sequence numbers. Returns an empty list if no events exist or match the criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Catch-up processing after system downtime
    /// public async Task CatchUpWithNewEventsAsync(Guid orderId, int lastProcessedSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var newEvents = await _context.GetDomainEventsFromSequence(
    ///         streamId, lastProcessedSequence + 1);
    ///     
    ///     if (newEvents.Count == 0)
    ///     {
    ///         _logger.LogInformation("No new events to process for order {OrderId}", orderId);
    ///         return;
    ///     }
    ///     
    ///     _logger.LogInformation("Processing {EventCount} new events for order {OrderId} starting from sequence {FromSequence}",
    ///         newEvents.Count, orderId, lastProcessedSequence + 1);
    ///     
    ///     foreach (var domainEvent in newEvents)
    ///     {
    ///         await ProcessEvent(domainEvent);
    ///         await UpdateCheckpoint(streamId.Id, domainEvent.Sequence);
    ///     }
    /// }
    /// 
    /// // Incremental projection updates
    /// public async Task&lt;Result&gt; UpdateProjectionIncrementallyAsync(
    ///     Guid orderId, 
    ///     int lastProjectionSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var projection = await _projectionStore.GetOrderProjectionAsync(orderId);
    ///     
    ///     if (projection == null)
    ///     {
    ///         return await BuildProjectionFromScratch(orderId);
    ///     }
    ///     
    ///     var newEvents = await _context.GetDomainEventsFromSequence(
    ///         streamId, lastProjectionSequence + 1);
    ///     
    ///     foreach (var domainEvent in newEvents)
    ///     {
    ///         ApplyEventToProjection(projection, domainEvent);
    ///     }
    ///     
    ///     projection.LastUpdatedSequence = newEvents.LastOrDefault()?.Sequence ?? lastProjectionSequence;
    ///     projection.LastUpdated = DateTime.UtcNow;
    ///     
    ///     await _projectionStore.SaveProjectionAsync(projection);
    ///     
    ///     _logger.LogInformation("Updated projection for order {OrderId} with {EventCount} new events",
    ///         orderId, newEvents.Count);
    ///     
    ///     return Result.Ok();
    /// }
    /// 
    /// // Event streaming with continuous processing
    /// public async Task StartEventStreamProcessingAsync(
    ///     IStreamId streamId, 
    ///     int startFromSequence,
    ///     CancellationToken cancellationToken)
    /// {
    ///     var currentSequence = startFromSequence;
    ///     
    ///     while (!cancellationToken.IsCancellationRequested)
    ///     {
    ///         var newEvents = await _context.GetDomainEventsFromSequence(
    ///             streamId, currentSequence, cancellationToken: cancellationToken);
    ///         
    ///         if (newEvents.Count &gt; 0)
    ///         {
    ///             foreach (var domainEvent in newEvents)
    ///             {
    ///                 await _eventProcessor.ProcessAsync(domainEvent);
    ///                 currentSequence = domainEvent.Sequence + 1;
    ///             }
    ///             
    ///             await SaveCheckpoint(streamId.Id, currentSequence - 1);
    ///         }
    ///         else
    ///         {
    ///             // No new events, wait before checking again
    ///             await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    ///         }
    ///     }
    /// }
    /// 
    /// // Differential analysis between two points in time
    /// public async Task&lt;EventDifferenceReport&gt; AnalyzeEventDifferencesAsync(
    ///     Guid orderId, 
    ///     int baselineSequence, 
    ///     int? comparisonSequence = null)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     
    ///     // Get events from baseline onwards
    ///     var eventsFromBaseline = await _context.GetDomainEventsFromSequence(
    ///         streamId, baselineSequence);
    ///     
    ///     List&lt;IDomainEvent&gt; relevantEvents;
    ///     if (comparisonSequence.HasValue)
    ///     {
    ///         // Filter to specific range
    ///         relevantEvents = eventsFromBaseline
    ///             .Where(e =&gt; e.Sequence &lt;= comparisonSequence.Value)
    ///             .ToList();
    ///     }
    ///     else
    ///     {
    ///         relevantEvents = eventsFromBaseline;
    ///     }
    ///     
    ///     return new EventDifferenceReport
    ///     {
    ///         OrderId = orderId,
    ///         BaselineSequence = baselineSequence,
    ///         ComparisonSequence = comparisonSequence ?? await _context.GetLatestEventSequence(streamId),
    ///         EventCount = relevantEvents.Count,
    ///         EventTypes = relevantEvents.GroupBy(e =&gt; e.GetType().Name)
    ///                                   .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         FirstNewEvent = relevantEvents.FirstOrDefault(),
    ///         LastNewEvent = relevantEvents.LastOrDefault(),
    ///         TimeSpan = relevantEvents.Count &gt; 0 
    ///             ? relevantEvents.Last().OccurredAt - relevantEvents.First().OccurredAt
    ///             : TimeSpan.Zero
    ///     };
    /// }
    /// 
    /// // Resume processing from checkpoint with error handling
    /// public async Task&lt;Result&gt; ResumeProcessingFromCheckpointAsync(string streamId)
    /// {
    ///     try
    ///     {
    ///         var checkpoint = await _checkpointStore.GetCheckpointAsync(streamId);
    ///         var fromSequence = checkpoint?.LastProcessedSequence + 1 ?? 1;
    ///         
    ///         var streamIdObj = new StreamId(streamId);
    ///         var events = await _context.GetDomainEventsFromSequence(streamIdObj, fromSequence);
    ///         
    ///         if (events.Count == 0)
    ///         {
    ///             _logger.LogInformation("Stream {StreamId} is up to date at sequence {Sequence}",
    ///                 streamId, fromSequence - 1);
    ///             return Result.Ok();
    ///         }
    ///         
    ///         _logger.LogInformation("Resuming processing of stream {StreamId} from sequence {FromSequence} with {EventCount} events",
    ///             streamId, fromSequence, events.Count);
    ///         
    ///         foreach (var domainEvent in events)
    ///         {
    ///             var processResult = await _eventHandler.HandleAsync(domainEvent);
    ///             if (processResult.IsNotSuccess)
    ///             {
    ///                 _logger.LogError("Failed to process event at sequence {Sequence}: {Error}",
    ///                     domainEvent.Sequence, processResult.Failure?.Description);
    ///                 return processResult;
    ///             }
    ///             
    ///             await _checkpointStore.UpdateCheckpointAsync(streamId, domainEvent.Sequence);
    ///         }
    ///         
    ///         return Result.Ok();
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         _logger.LogError(ex, "Error resuming processing for stream {StreamId}", streamId);
    ///         return new Failure("Resume failed", ex.Message);
    ///     }
    /// }
    /// 
    /// // Batch processing with type filtering for specific event handlers
    /// public async Task ProcessSpecificEventTypesAsync(
    ///     IStreamId streamId, 
    ///     int fromSequence,
    ///     Type[] eventTypesToProcess)
    /// {
    ///     var events = await _context.GetDomainEventsFromSequence(
    ///         streamId, fromSequence, eventTypesToProcess);
    ///     
    ///     var eventGroups = events.GroupBy(e =&gt; e.GetType()).ToList();
    ///     
    ///     foreach (var eventGroup in eventGroups)
    ///     {
    ///         var eventType = eventGroup.Key;
    ///         var eventsOfType = eventGroup.OrderBy(e =&gt; e.Sequence).ToList();
    ///         
    ///         var handlerType = typeof(IEventHandler&lt;&gt;).MakeGenericType(eventType);
    ///         var handler = _serviceProvider.GetService(handlerType);
    ///         
    ///         if (handler != null)
    ///         {
    ///             _logger.LogInformation("Processing {Count} events of type {EventType} starting from sequence {FromSequence}",
    ///                 eventsOfType.Count, eventType.Name, eventsOfType.First().Sequence);
    ///             
    ///             foreach (var domainEvent in eventsOfType)
    ///             {
    ///                 await ((dynamic)handler).HandleAsync((dynamic)domainEvent);
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<List<IDomainEvent>> GetDomainEventsFromSequence(this IDomainDbContext domainDbContext, IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesFromSequence(streamId, fromSequence, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
