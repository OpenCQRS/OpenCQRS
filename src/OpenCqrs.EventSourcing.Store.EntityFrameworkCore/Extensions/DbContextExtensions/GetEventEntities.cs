using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all event entities from a specified stream, with optional filtering by event types.
    /// This method provides access to the raw event entity storage format, supporting scenarios that
    /// require direct entity manipulation, performance optimization, or custom event processing workflows.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to the event store entities and querying
    /// capabilities for retrieving events from the underlying database storage.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose event entities should be retrieved.
    /// All event entities associated with this stream will be considered for inclusion.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the results. When provided, only event entities
    /// representing events that match the specified types will be included in the returned collection.
    /// If null or empty, all event entities in the stream are returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="EventEntity"/> objects representing all events in the stream
    /// that match the optional filter criteria, ordered by their sequence numbers.
    /// Returns an empty list if no event entities exist or match the filter criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get all event entities for direct processing
    /// public async Task&lt;List&lt;EventEntity&gt;&gt; GetRawEventDataAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var eventEntities = await _context.GetEventEntities(streamId);
    ///     
    ///     _logger.LogInformation("Retrieved {Count} raw event entities for order {OrderId}",
    ///         eventEntities.Count, orderId);
    ///     
    ///     return eventEntities;
    /// }
    /// 
    /// // Filter for specific event types for performance optimization
    /// public async Task&lt;List&lt;EventEntity&gt;&gt; GetPaymentEventEntitiesAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var paymentEventTypes = new[] 
    ///     { 
    ///         typeof(PaymentInitiatedEvent),
    ///         typeof(PaymentCompletedEvent),
    ///         typeof(PaymentFailedEvent)
    ///     };
    ///     
    ///     var eventEntities = await _context.GetEventEntities(streamId, paymentEventTypes);
    ///     return eventEntities;
    /// }
    /// 
    /// // Custom event processing with entity metadata
    /// public async Task ProcessEventEntitiesWithMetadataAsync(IStreamId streamId)
    /// {
    ///     var eventEntities = await _context.GetEventEntities(streamId);
    ///     
    ///     foreach (var entity in eventEntities)
    ///     {
    ///         // Access raw storage format for custom processing
    ///         var eventData = JsonSerializer.Deserialize&lt;Dictionary&lt;string, object&gt;&gt;(entity.Data);
    ///         var metadata = entity.Metadata ?? new Dictionary&lt;string, object&gt;();
    ///         
    ///         await ProcessRawEventData(entity.TypeName, eventData, metadata, entity.OccurredAt);
    ///         
    ///         _logger.LogDebug("Processed event {EventType} at sequence {Sequence}",
    ///             entity.TypeName, entity.Sequence);
    ///     }
    /// }
    /// 
    /// // Performance comparison between entity and domain event retrieval
    /// public async Task&lt;PerformanceComparison&gt; CompareRetrievalMethodsAsync(IStreamId streamId)
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var eventEntities = await _context.GetEventEntities(streamId);
    ///     stopwatch.Stop();
    ///     var entityRetrievalTime = stopwatch.Elapsed;
    ///     
    ///     stopwatch.Restart();
    ///     var domainEvents = await _context.GetDomainEvents(streamId);
    ///     stopwatch.Stop();
    ///     var domainEventRetrievalTime = stopwatch.Elapsed;
    ///     
    ///     return new PerformanceComparison
    ///     {
    ///         EntityCount = eventEntities.Count,
    ///         EntityRetrievalTime = entityRetrievalTime,
    ///         DomainEventRetrievalTime = domainEventRetrievalTime,
    ///         DeserializationOverhead = domainEventRetrievalTime - entityRetrievalTime
    ///     };
    /// }
    /// 
    /// // Export events in raw format for migration or backup
    /// public async Task&lt;EventExportResult&gt; ExportEventEntitiesAsync(
    ///     IStreamId streamId,
    ///     string exportFormat = "json")
    /// {
    ///     var eventEntities = await _context.GetEventEntities(streamId);
    ///     
    ///     var exportData = eventEntities.Select(entity =&gt; new
    ///     {
    ///         entity.Id,
    ///         entity.StreamId,
    ///         entity.Sequence,
    ///         entity.TypeName,
    ///         entity.TypeVersion,
    ///         entity.Data,
    ///         entity.Metadata,
    ///         entity.OccurredAt
    ///     }).ToList();
    ///     
    ///     var serializedData = exportFormat.ToLower() switch
    ///     {
    ///         "json" =&gt; JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }),
    ///         "xml" =&gt; SerializeToXml(exportData),
    ///         _ =&gt; throw new ArgumentException($"Unsupported export format: {exportFormat}")
    ///     };
    ///     
    ///     return new EventExportResult
    ///     {
    ///         StreamId = streamId.Id,
    ///         EventCount = eventEntities.Count,
    ///         ExportFormat = exportFormat,
    ///         SerializedData = serializedData,
    ///         ExportTimestamp = DateTime.UtcNow
    ///     };
    /// }
    /// 
    /// // Event store statistics and analysis
    /// public async Task&lt;EventStoreStatistics&gt; AnalyzeEventStoreAsync(IStreamId streamId)
    /// {
    ///     var eventEntities = await _context.GetEventEntities(streamId);
    ///     
    ///     var statistics = new EventStoreStatistics
    ///     {
    ///         StreamId = streamId.Id,
    ///         TotalEvents = eventEntities.Count,
    ///         EventTypeDistribution = eventEntities
    ///             .GroupBy(e =&gt; e.TypeName)
    ///             .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         TypeVersionDistribution = eventEntities
    ///             .GroupBy(e =&gt; $"{e.TypeName} v{e.TypeVersion}")
    ///             .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         SequenceRange = eventEntities.Count &gt; 0 
    ///             ? (eventEntities.First().Sequence, eventEntities.Last().Sequence)
    ///             : (0, 0),
    ///         TimeRange = eventEntities.Count &gt; 0
    ///             ? (eventEntities.First().OccurredAt, eventEntities.Last().OccurredAt)
    ///             : (DateTime.MinValue, DateTime.MaxValue),
    ///         AverageEventSize = eventEntities.Count &gt; 0
    ///             ? eventEntities.Average(e =&gt; e.Data.Length)
    ///             : 0,
    ///         EventsWithMetadata = eventEntities.Count(e =&gt; e.Metadata != null && e.Metadata.Count &gt; 0)
    ///     };
    ///     
    ///     return statistics;
    /// }
    /// </code>
    /// </example>
    public static async Task<List<EventEntity>> GetEventEntities(this IDomainDbContext domainDbContext, IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var filterEvents = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEvents)
        {
            return await domainDbContext.Events.AsNoTracking()
                .Where(eventEntity => eventEntity.StreamId == streamId.Id)
                .OrderBy(eventEntity => eventEntity.Sequence)
                .ToListAsync(cancellationToken);
        }

        var eventTypes = eventTypeFilter!.Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType)).Select(b => b.Key).ToList();
        return await domainDbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventTypes.Contains(eventEntity.EventType))
            .OrderBy(eventEntity => eventEntity.Sequence)
            .ToListAsync(cancellationToken);
    }
}
