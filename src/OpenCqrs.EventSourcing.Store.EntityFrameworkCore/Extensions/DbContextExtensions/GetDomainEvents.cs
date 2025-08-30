using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all domain events from a specified stream, with optional filtering by event types.
    /// This method provides comprehensive access to the complete event history within a stream,
    /// supporting event replay, analysis, and projection scenarios in event sourcing systems.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event entities and deserialization
    /// capabilities for converting stored events back to domain event objects.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose events should be retrieved.
    /// All events associated with this stream will be considered for inclusion.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the results. When provided, only events
    /// matching the specified types will be included in the returned collection.
    /// If null or empty, all events in the stream are returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="IDomainEvent"/> objects representing all events in the stream
    /// that match the optional filter criteria, ordered by their sequence numbers.
    /// Returns an empty list if no events exist or match the filter criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get all events for a specific aggregate
    /// public async Task&lt;List&lt;IDomainEvent&gt;&gt; GetOrderHistoryAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     
    ///     _logger.LogInformation("Retrieved {EventCount} events for order {OrderId}", 
    ///         events.Count, orderId);
    ///     
    ///     return events;
    /// }
    /// 
    /// // Get specific types of events for analysis
    /// public async Task&lt;List&lt;IDomainEvent&gt;&gt; GetOrderPaymentEventsAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var paymentEventTypes = new[] 
    ///     { 
    ///         typeof(PaymentInitiatedEvent),
    ///         typeof(PaymentCompletedEvent),
    ///         typeof(PaymentFailedEvent)
    ///     };
    ///     
    ///     var events = await _context.GetDomainEvents(streamId, paymentEventTypes);
    ///     return events;
    /// }
    /// 
    /// // Event stream analysis for business intelligence
    /// public async Task&lt;OrderAnalytics&gt; AnalyzeOrderPatternAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     
    ///     var analytics = new OrderAnalytics
    ///     {
    ///         OrderId = orderId,
    ///         TotalEvents = events.Count,
    ///         EventTypes = events.GroupBy(e =&gt; e.GetType().Name)
    ///                          .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         FirstEvent = events.FirstOrDefault(),
    ///         LastEvent = events.LastOrDefault(),
    ///         ProcessingTime = events.LastOrDefault()?.OccurredAt - events.FirstOrDefault()?.OccurredAt
    ///     };
    ///     
    ///     return analytics;
    /// }
    /// 
    /// // Build projection from event stream
    /// public async Task&lt;OrderSummaryProjection&gt; BuildOrderSummaryAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     
    ///     var projection = new OrderSummaryProjection { OrderId = orderId };
    ///     
    ///     foreach (var domainEvent in events)
    ///     {
    ///         switch (domainEvent)
    ///         {
    ///             case OrderCreatedEvent created:
    ///                 projection.CreatedAt = created.CreatedAt;
    ///                 projection.CustomerId = created.CustomerId;
    ///                 projection.Status = OrderStatus.Created;
    ///                 break;
    ///                 
    ///             case OrderItemAddedEvent itemAdded:
    ///                 projection.TotalAmount += itemAdded.UnitPrice * itemAdded.Quantity;
    ///                 projection.ItemCount++;
    ///                 break;
    ///                 
    ///             case OrderCompletedEvent completed:
    ///                 projection.CompletedAt = completed.CompletedAt;
    ///                 projection.Status = OrderStatus.Completed;
    ///                 break;
    ///         }
    ///     }
    ///     
    ///     return projection;
    /// }
    /// 
    /// // Event replay for system migration
    /// public async Task&lt;Result&gt; ReplayEventsToNewSystemAsync(IStreamId sourceStreamId, IEventPublisher publisher)
    /// {
    ///     try
    ///     {
    ///         var events = await _context.GetDomainEvents(sourceStreamId);
    ///         
    ///         _logger.LogInformation("Starting replay of {EventCount} events from stream {StreamId}",
    ///             events.Count, sourceStreamId.Id);
    ///         
    ///         var batchSize = 100;
    ///         for (int i = 0; i &lt; events.Count; i += batchSize)
    ///         {
    ///             var batch = events.Skip(i).Take(batchSize).ToList();
    ///             
    ///             foreach (var domainEvent in batch)
    ///             {
    ///                 await publisher.PublishAsync(domainEvent);
    ///             }
    ///             
    ///             _logger.LogDebug("Replayed events {From}-{To} of {Total}",
    ///                 i + 1, Math.Min(i + batchSize, events.Count), events.Count);
    ///         }
    ///         
    ///         return Result.Ok();
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         _logger.LogError(ex, "Failed to replay events from stream {StreamId}", sourceStreamId.Id);
    ///         return new Failure("Replay failed", ex.Message);
    ///     }
    /// }
    /// 
    /// // Audit trail generation
    /// public async Task&lt;AuditTrail&gt; GenerateAuditTrailAsync(IStreamId streamId, DateTime? fromDate = null)
    /// {
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     
    ///     if (fromDate.HasValue)
    ///     {
    ///         events = events.Where(e =&gt; e.OccurredAt &gt;= fromDate.Value).ToList();
    ///     }
    ///     
    ///     var auditTrail = new AuditTrail
    ///     {
    ///         StreamId = streamId.Id,
    ///         GeneratedAt = DateTime.UtcNow,
    ///         EventCount = events.Count,
    ///         DateRange = events.Count &gt; 0 
    ///             ? (events.First().OccurredAt, events.Last().OccurredAt)
    ///             : (DateTime.MinValue, DateTime.MinValue),
    ///         EventSummary = events
    ///             .GroupBy(e =&gt; new { e.GetType().Name, e.OccurredAt.Date })
    ///             .Select(g =&gt; new AuditEntry
    ///             {
    ///                 Date = g.Key.Date,
    ///                 EventType = g.Key.Name,
    ///                 Count = g.Count(),
    ///                 Events = g.ToList()
    ///             })
    ///             .OrderBy(ae =&gt; ae.Date)
    ///             .ThenBy(ae =&gt; ae.EventType)
    ///             .ToList()
    ///     };
    ///     
    ///     return auditTrail;
    /// }
    /// 
    /// // Event type distribution analysis
    /// public async Task&lt;Dictionary&lt;string, EventTypeMetrics&gt;&gt; GetEventTypeDistributionAsync(
    ///     IStreamId streamId)
    /// {
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     
    ///     return events
    ///         .GroupBy(e =&gt; e.GetType().Name)
    ///         .ToDictionary(
    ///             g =&gt; g.Key,
    ///             g =&gt; new EventTypeMetrics
    ///             {
    ///                 Count = g.Count(),
    ///                 FirstOccurrence = g.Min(e =&gt; e.OccurredAt),
    ///                 LastOccurrence = g.Max(e =&gt; e.OccurredAt),
    ///                 AverageFrequency = CalculateFrequency(g.ToList())
    ///             }
    ///         );
    /// }
    /// </code>
    /// </example>
    public static async Task<List<IDomainEvent>> GetDomainEvents(this IDomainDbContext domainDbContext, IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntities(streamId, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
