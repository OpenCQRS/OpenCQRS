using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves event entities from a specified stream up to and including a specific sequence number,
    /// with optional filtering by event types. This method provides access to the raw event entity storage
    /// format for time-travel scenarios, historical analysis, and performance-optimized event processing workflows.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to the event store entities and querying
    /// capabilities for retrieving events from the underlying database storage.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose event entities should be retrieved.
    /// Event entities will be filtered to only include those from this specific stream.
    /// </param>
    /// <param name="upToSequence">
    /// The maximum sequence number (inclusive) for event entities to be included in the results.
    /// Only event entities with sequence numbers less than or equal to this value will be returned,
    /// enabling point-in-time queries and historical state reconstruction scenarios.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the results. When provided, only event entities
    /// representing events that match the specified types will be included in the returned collection.
    /// If null or empty, all event entities up to the sequence limit are returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="EventEntity"/> objects representing events in the stream
    /// up to the specified sequence number that match the optional filter criteria,
    /// ordered by their sequence numbers. Returns an empty list if no event entities exist or match the criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get raw event entities for point-in-time analysis
    /// public async Task&lt;List&lt;EventEntity&gt;&gt; GetEventEntitiesAtPointInTimeAsync(Guid orderId, int targetSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var eventEntities = await _context.GetEventEntitiesUpToSequence(streamId, targetSequence);
    ///     
    ///     _logger.LogInformation("Retrieved {Count} event entities up to sequence {Sequence} for order {OrderId}",
    ///         eventEntities.Count, targetSequence, orderId);
    ///     
    ///     return eventEntities;
    /// }
    /// 
    /// // Performance-optimized historical data processing
    /// public async Task&lt;EventProcessingReport&gt; ProcessHistoricalEventsAsync(
    ///     IStreamId streamId, 
    ///     int upToSequence, 
    ///     Type[] relevantEventTypes)
    /// {
    ///     var eventEntities = await _context.GetEventEntitiesUpToSequence(
    ///         streamId, upToSequence, relevantEventTypes);
    ///     
    ///     var report = new EventProcessingReport
    ///     {
    ///         StreamId = streamId.Id,
    ///         ProcessedUpToSequence = upToSequence,
    ///         TotalEntitiesProcessed = eventEntities.Count,
    ///         ProcessedEventTypes = eventEntities.GroupBy(e =&gt; e.TypeName).ToDictionary(g =&gt; g.Key, g =&gt; g.Count())
    ///     };
    ///     
    ///     foreach (var entity in eventEntities)
    ///     {
    ///         await ProcessEventEntity(entity);
    ///     }
    ///     
    ///     return report;
    /// }
    /// 
    /// // Time-travel debugging with raw entity inspection
    /// public async Task&lt;EntityDiagnosticReport&gt; DiagnoseEntityStateAsync(Guid orderId, int problemSequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var entitiesUpToProblem = await _context.GetEventEntitiesUpToSequence(streamId, problemSequence);
    ///     
    ///     var diagnostic = new EntityDiagnosticReport
    ///     {
    ///         OrderId = orderId,
    ///         InvestigatedSequence = problemSequence,
    ///         EntityCount = entitiesUpToProblem.Count
    ///     };
    ///     
    ///     // Analyze raw entity data for anomalies
    ///     foreach (var entity in entitiesUpToProblem.TakeLast(10)) // Last 10 events before problem
    ///     {
    ///         diagnostic.RecentEntities.Add(new EntityDiagnostic
    ///         {
    ///             Sequence = entity.Sequence,
    ///             TypeName = entity.TypeName,
    ///             TypeVersion = entity.TypeVersion,
    ///             DataSize = entity.Data.Length,
    ///             HasMetadata = entity.Metadata?.Count &gt; 0,
    ///             OccurredAt = entity.OccurredAt,
    ///             RawData = entity.Data // For detailed inspection
    ///         });
    ///     }
    ///     
    ///     return diagnostic;
    /// }
    /// 
    /// // Compliance audit with filtered entity retrieval
    /// public async Task&lt;ComplianceAuditResult&gt; PerformComplianceAuditAsync(
    ///     IStreamId streamId,
    ///     DateTime auditCutoffDate,
    ///     Type[] complianceEventTypes)
    /// {
    ///     // First, determine the sequence number at the cutoff date
    ///     var allEvents = await _context.GetEventEntities(streamId);
    ///     var cutoffSequence = allEvents
    ///         .Where(e =&gt; e.OccurredAt &lt;= auditCutoffDate)
    ///         .LastOrDefault()?.Sequence ?? 0;
    ///     
    ///     if (cutoffSequence == 0)
    ///     {
    ///         return new ComplianceAuditResult 
    ///         { 
    ///             StreamId = streamId.Id,
    ///             Status = "No Events",
    ///             Message = "No events existed at the audit cutoff date"
    ///         };
    ///     }
    ///     
    ///     // Get filtered compliance-relevant entities up to cutoff
    ///     var complianceEntities = await _context.GetEventEntitiesUpToSequence(
    ///         streamId, cutoffSequence, complianceEventTypes);
    ///     
    ///     return new ComplianceAuditResult
    ///     {
    ///         StreamId = streamId.Id,
    ///         AuditCutoffDate = auditCutoffDate,
    ///         CutoffSequence = cutoffSequence,
    ///         ComplianceEventCount = complianceEntities.Count,
    ///         ComplianceEvents = complianceEntities.Select(e =&gt; new ComplianceEventSummary
    ///         {
    ///             Sequence = e.Sequence,
    ///             EventType = e.TypeName,
    ///             OccurredAt = e.OccurredAt,
    ///             ComplianceRelevant = true
    ///         }).ToList(),
    ///         Status = "Complete"
    ///     };
    /// }
    /// 
    /// // Bulk data migration with sequence chunking
    /// public async Task&lt;MigrationResult&gt; MigrateEventDataAsync(
    ///     IStreamId sourceStreamId,
    ///     IStreamId targetStreamId,
    ///     int maxSequenceToMigrate,
    ///     int batchSize = 1000)
    /// {
    ///     var migrationResult = new MigrationResult { SourceStreamId = sourceStreamId.Id, TargetStreamId = targetStreamId.Id };
    ///     var processedCount = 0;
    ///     
    ///     for (int startSequence = 1; startSequence &lt;= maxSequenceToMigrate; startSequence += batchSize)
    ///     {
    ///         var endSequence = Math.Min(startSequence + batchSize - 1, maxSequenceToMigrate);
    ///         
    ///         var entitiesToMigrate = await _context.GetEventEntitiesUpToSequence(sourceStreamId, endSequence);
    ///         var batchEntities = entitiesToMigrate.Where(e =&gt; e.Sequence &gt;= startSequence).ToList();
    ///         
    ///         foreach (var entity in batchEntities)
    ///         {
    ///             var migratedEntity = new EventEntity
    ///             {
    ///                 StreamId = targetStreamId.Id,
    ///                 Sequence = entity.Sequence,
    ///                 TypeName = entity.TypeName,
    ///                 TypeVersion = entity.TypeVersion,
    ///                 Data = TransformEventData(entity.Data), // Apply migration transformations
    ///                 Metadata = AddMigrationMetadata(entity.Metadata),
    ///                 OccurredAt = entity.OccurredAt
    ///             };
    ///             
    ///             await _targetContext.Events.AddAsync(migratedEntity);
    ///             processedCount++;
    ///         }
    ///         
    ///         await _targetContext.SaveChangesAsync();
    ///         
    ///         _logger.LogInformation("Migrated entities {Start}-{End} of {Total}",
    ///             startSequence, endSequence, maxSequenceToMigrate);
    ///     }
    ///     
    ///     migrationResult.TotalMigrated = processedCount;
    ///     migrationResult.Status = "Complete";
    ///     return migrationResult;
    /// }
    /// 
    /// // Performance benchmarking with sequence-limited queries
    /// public async Task&lt;QueryPerformanceMetrics&gt; BenchmarkSequencedQueriesAsync(
    ///     IStreamId streamId,
    ///     int[] sequenceTestPoints)
    /// {
    ///     var metrics = new QueryPerformanceMetrics { StreamId = streamId.Id };
    ///     
    ///     foreach (var sequence in sequenceTestPoints)
    ///     {
    ///         var stopwatch = Stopwatch.StartNew();
    ///         
    ///         var entities = await _context.GetEventEntitiesUpToSequence(streamId, sequence);
    ///         
    ///         stopwatch.Stop();
    ///         
    ///         metrics.SequencePerformanceData.Add(sequence, new PerformanceDataPoint
    ///         {
    ///             Sequence = sequence,
    ///             EntityCount = entities.Count,
    ///             QueryTime = stopwatch.Elapsed,
    ///             MemoryUsed = GC.GetTotalMemory(false) // Rough memory usage indicator
    ///         });
    ///     }
    ///     
    ///     return metrics;
    /// }
    /// 
    /// // Event stream validation with entity-level checks
    /// public async Task&lt;StreamValidationResult&gt; ValidateEventStreamIntegrityAsync(
    ///     IStreamId streamId,
    ///     int upToSequence)
    /// {
    ///     var entities = await _context.GetEventEntitiesUpToSequence(streamId, upToSequence);
    ///     var validationResult = new StreamValidationResult { StreamId = streamId.Id };
    ///     
    ///     // Check sequence continuity
    ///     for (int i = 0; i &lt; entities.Count; i++)
    ///     {
    ///         var expectedSequence = i + 1;
    ///         if (entities[i].Sequence != expectedSequence)
    ///         {
    ///             validationResult.Errors.Add($"Sequence gap detected: expected {expectedSequence}, found {entities[i].Sequence}");
    ///         }
    ///         
    ///         // Validate entity data integrity
    ///         if (string.IsNullOrEmpty(entities[i].Data))
    ///         {
    ///             validationResult.Errors.Add($"Empty event data at sequence {entities[i].Sequence}");
    ///         }
    ///         
    ///         // Check timestamp ordering
    ///         if (i &gt; 0 && entities[i].OccurredAt &lt; entities[i - 1].OccurredAt)
    ///         {
    ///             validationResult.Warnings.Add($"Timestamp ordering violation at sequence {entities[i].Sequence}");
    ///         }
    ///     }
    ///     
    ///     validationResult.IsValid = validationResult.Errors.Count == 0;
    ///     validationResult.TotalEntitiesValidated = entities.Count;
    ///     
    ///     return validationResult;
    /// }
    /// </code>
    /// </example>
    public static async Task<List<EventEntity>> GetEventEntitiesUpToSequence(this IDomainDbContext domainDbContext, IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            return await domainDbContext.Events.AsNoTracking()
                .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence <= upToSequence)
                .OrderBy(eventEntity => eventEntity.Sequence)
                .ToListAsync(cancellationToken);
        }

        var domainEventTypeKeys = eventTypeFilter!
            .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
            .Select(b => b.Key).ToList();

        return await domainDbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence <= upToSequence && domainEventTypeKeys.Contains(eventEntity.EventType))
            .OrderBy(eventEntity => eventEntity.Sequence)
            .ToListAsync(cancellationToken);
    }
}
