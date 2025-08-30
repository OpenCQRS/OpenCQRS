using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all aggregate-event relationship entities associated with a specific aggregate instance, providing
    /// complete visibility into the many-to-many relationships between the aggregate and its applied events.
    /// This method returns junction table entities that include both relationship metadata and navigation properties
    /// to the associated event entities, enabling comprehensive aggregate-event relationship analysis.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate whose event relationships should be retrieved. Must implement <see cref="IAggregate"/>,
    /// have a parameterless constructor for type instantiation, and be decorated with the <see cref="AggregateType"/>
    /// attribute for proper versioned identifier resolution and metadata extraction.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to the aggregate-event relationship entities
    /// through the <see cref="IDomainDbContext.AggregateEvents"/> DbSet, including navigation properties
    /// for efficient querying and data retrieval operations.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate whose event relationships should be retrieved.
    /// This identifier is combined with the aggregate type version to create a versioned lookup key
    /// that ensures accurate relationship retrieval across aggregate type evolution scenarios.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous database operation if needed,
    /// supporting graceful shutdown scenarios and preventing resource leaks during long-running queries.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either a list of <see cref="AggregateEventEntity"/> instances
    /// representing all aggregate-event relationships for the specified aggregate, including eagerly loaded
    /// event entities through navigation properties, or a <see cref="Failure"/> if the operation failed
    /// due to missing aggregate type metadata or database access issues.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the aggregate type does not have the required <see cref="AggregateType"/> attribute.
    /// This attribute is essential for creating the versioned aggregate identifier used in relationship lookups
    /// and ensures proper aggregate type metadata resolution.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via the provided <paramref name="cancellationToken"/>,
    /// typically during application shutdown or when operation timeouts are exceeded.
    /// </exception>
    /// <example>
    /// <code>
    /// // Basic retrieval of aggregate-event relationships
    /// public async Task&lt;Result&lt;AggregateEventSummary&gt;&gt; GetEventRelationshipSummaryAsync(Guid orderId)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId);
    ///     if (relationshipsResult.IsNotSuccess)
    ///         return relationshipsResult.Failure!;
    ///         
    ///     var relationships = relationshipsResult.Value!;
    ///     
    ///     return new AggregateEventSummary
    ///     {
    ///         AggregateId = orderId,
    ///         TotalEventRelationships = relationships.Count,
    ///         EventTypes = relationships.Select(r =&gt; $"{r.Event.TypeName} v{r.Event.TypeVersion}").Distinct().ToList(),
    ///         FirstEventApplied = relationships.Min(r =&gt; r.AppliedDate),
    ///         LastEventApplied = relationships.Max(r =&gt; r.AppliedDate),
    ///         EventSequenceRange = new Range(
    ///             relationships.Min(r =&gt; r.Event.Sequence),
    ///             relationships.Max(r =&gt; r.Event.Sequence)
    ///         )
    ///     };
    /// }
    /// 
    /// // Audit trail generation with relationship details
    /// public async Task&lt;Result&lt;AggregateAuditTrail&gt;&gt; GenerateComprehensiveAuditTrailAsync&lt;T&gt;(
    ///     IAggregateId&lt;T&gt; aggregateId) where T : IAggregate, new()
    /// {
    ///     var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId);
    ///     if (relationshipsResult.IsNotSuccess)
    ///         return relationshipsResult.Failure!;
    ///         
    ///     var relationships = relationshipsResult.Value!
    ///         .OrderBy(r =&gt; r.AppliedDate)
    ///         .ToList();
    ///     
    ///     var auditEntries = relationships.Select(r =&gt; new AuditTrailEntry
    ///     {
    ///         EventId = r.EventId,
    ///         EventType = r.Event.TypeName,
    ///         EventVersion = r.Event.TypeVersion,
    ///         EventSequence = r.Event.Sequence,
    ///         EventData = r.Event.Data,
    ///         EventCreated = r.Event.CreatedDate,
    ///         AppliedToAggregate = r.AppliedDate,
    ///         ApplicationDelay = r.AppliedDate - r.Event.CreatedDate,
    ///         RelationshipId = $"{r.AggregateId}|{r.EventId}"
    ///     }).ToList();
    /// 
    ///     return new AggregateAuditTrail
    ///     {
    ///         AggregateId = aggregateId.Id,
    ///         TrailEntries = auditEntries,
    ///         Summary = new AuditSummary
    ///         {
    ///             TotalEvents = auditEntries.Count,
    ///             EventTypeDistribution = auditEntries.GroupBy(e =&gt; e.EventType)
    ///                 .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///             AverageApplicationDelay = TimeSpan.FromMilliseconds(
    ///                 auditEntries.Average(e =&gt; e.ApplicationDelay.TotalMilliseconds)),
    ///             TrailTimeSpan = auditEntries.Max(e =&gt; e.AppliedToAggregate) - 
    ///                            auditEntries.Min(e =&gt; e.AppliedToAggregate)
    ///         }
    ///     };
    /// }
    /// 
    /// // Performance analysis of event application patterns
    /// public async Task&lt;Result&lt;EventApplicationMetrics&gt;&gt; AnalyzeEventApplicationPatternsAsync&lt;T&gt;(
    ///     IAggregateId&lt;T&gt; aggregateId) where T : IAggregate, new()
    /// {
    ///     var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId);
    ///     if (relationshipsResult.IsNotSuccess)
    ///         return relationshipsResult.Failure!;
    ///         
    ///     var relationships = relationshipsResult.Value!;
    ///     
    ///     var applicationTimes = relationships
    ///         .Select(r =&gt; r.AppliedDate - r.Event.CreatedDate)
    ///         .ToList();
    ///         
    ///     var sequenceGaps = relationships
    ///         .OrderBy(r =&gt; r.Event.Sequence)
    ///         .Zip(relationships.OrderBy(r =&gt; r.Event.Sequence).Skip(1), 
    ///              (current, next) =&gt; next.Event.Sequence - current.Event.Sequence - 1)
    ///         .ToList();
    /// 
    ///     return new EventApplicationMetrics
    ///     {
    ///         AggregateId = aggregateId.Id,
    ///         TotalEventRelationships = relationships.Count,
    ///         AverageApplicationDelay = TimeSpan.FromMilliseconds(
    ///             applicationTimes.Average(t =&gt; t.TotalMilliseconds)),
    ///         MaxApplicationDelay = applicationTimes.Max(),
    ///         MinApplicationDelay = applicationTimes.Min(),
    ///         ImmediateApplications = applicationTimes.Count(t =&gt; t.TotalSeconds &lt; 1),
    ///         DelayedApplications = applicationTimes.Count(t =&gt; t.TotalMinutes &gt; 1),
    ///         AverageSequenceGap = sequenceGaps.Any() ? sequenceGaps.Average() : 0,
    ///         MaxSequenceGap = sequenceGaps.Any() ? sequenceGaps.Max() : 0,
    ///         OutOfSequenceEvents = relationships.Count(r =&gt; 
    ///             r.AppliedDate &lt; r.Event.CreatedDate) // Applied before creation (unusual)
    ///     };
    /// }
    /// 
    /// // Debugging aggregate state reconstruction issues
    /// public async Task&lt;Result&gt; DiagnoseAggregateStateIssuesAsync&lt;T&gt;(
    ///     IAggregateId&lt;T&gt; aggregateId, 
    ///     ILogger logger) where T : IAggregate, new()
    /// {
    ///     var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId);
    ///     if (relationshipsResult.IsNotSuccess)
    ///     {
    ///         logger.LogError("Failed to retrieve aggregate event relationships for {AggregateId}: {Error}",
    ///             aggregateId.Id, relationshipsResult.Failure!.Description);
    ///         return relationshipsResult.Failure!;
    ///     }
    ///     
    ///     var relationships = relationshipsResult.Value!
    ///         .OrderBy(r =&gt; r.Event.Sequence)
    ///         .ToList();
    /// 
    ///     logger.LogInformation("Aggregate {AggregateId} has {Count} event relationships",
    ///         aggregateId.Id, relationships.Count);
    /// 
    ///     // Check for sequence gaps
    ///     var sequences = relationships.Select(r =&gt; r.Event.Sequence).ToList();
    ///     var expectedSequences = Enumerable.Range(sequences.Min(), sequences.Count).ToList();
    ///     var missingSequences = expectedSequences.Except(sequences).ToList();
    ///     
    ///     if (missingSequences.Any())
    ///     {
    ///         logger.LogWarning("Missing event sequences detected for aggregate {AggregateId}: {MissingSequences}",
    ///             aggregateId.Id, string.Join(", ", missingSequences));
    ///     }
    /// 
    ///     // Check for duplicate relationships
    ///     var duplicateEvents = relationships
    ///         .GroupBy(r =&gt; r.EventId)
    ///         .Where(g =&gt; g.Count() &gt; 1)
    ///         .Select(g =&gt; g.Key)
    ///         .ToList();
    ///         
    ///     if (duplicateEvents.Any())
    ///     {
    ///         logger.LogError("Duplicate event relationships detected for aggregate {AggregateId}: {DuplicateEvents}",
    ///             aggregateId.Id, string.Join(", ", duplicateEvents));
    ///     }
    /// 
    ///     // Check application timing anomalies
    ///     var anomalies = relationships
    ///         .Where(r =&gt; r.AppliedDate &lt; r.Event.CreatedDate)
    ///         .ToList();
    ///         
    ///     if (anomalies.Any())
    ///     {
    ///         logger.LogWarning("Event application timing anomalies detected for aggregate {AggregateId}: {Count} events applied before creation",
    ///             aggregateId.Id, anomalies.Count);
    ///     }
    /// 
    ///     return Result.Ok();
    /// }
    /// 
    /// // Export aggregate-event relationships for external analysis
    /// public async Task&lt;Result&lt;AggregateEventExport&gt;&gt; ExportAggregateEventRelationshipsAsync&lt;T&gt;(
    ///     IAggregateId&lt;T&gt; aggregateId, 
    ///     ExportFormat format = ExportFormat.Json) where T : IAggregate, new()
    /// {
    ///     var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId);
    ///     if (relationshipsResult.IsNotSuccess)
    ///         return relationshipsResult.Failure!;
    ///         
    ///     var relationships = relationshipsResult.Value!
    ///         .OrderBy(r =&gt; r.AppliedDate)
    ///         .ToList();
    /// 
    ///     var exportData = relationships.Select(r =&gt; new AggregateEventRelationshipExport
    ///     {
    ///         AggregateId = r.AggregateId,
    ///         EventId = r.EventId,
    ///         EventType = r.Event.TypeName,
    ///         EventVersion = r.Event.TypeVersion,
    ///         EventSequence = r.Event.Sequence,
    ///         EventCreated = r.Event.CreatedDate,
    ///         EventApplied = r.AppliedDate,
    ///         ApplicationDelay = (r.AppliedDate - r.Event.CreatedDate).TotalMilliseconds,
    ///         EventData = format == ExportFormat.Full ? r.Event.Data : null,
    ///         EventDataSize = r.Event.Data?.Length ?? 0
    ///     }).ToList();
    /// 
    ///     var exportContent = format switch
    ///     {
    ///         ExportFormat.Json =&gt; JsonConvert.SerializeObject(exportData, Formatting.Indented),
    ///         ExportFormat.Csv =&gt; ConvertToCsv(exportData),
    ///         ExportFormat.Xml =&gt; SerializeToXml(exportData),
    ///         _ =&gt; JsonConvert.SerializeObject(exportData)
    ///     };
    /// 
    ///     return new AggregateEventExport
    ///     {
    ///         AggregateId = aggregateId.Id,
    ///         ExportFormat = format,
    ///         RelationshipCount = exportData.Count,
    ///         ExportContent = exportContent,
    ///         GeneratedAt = DateTimeOffset.UtcNow,
    ///         ContentSize = exportContent.Length
    ///     };
    /// }
    /// 
    /// // Batch processing for multiple aggregates
    /// public async Task&lt;Result&lt;Dictionary&lt;string, List&lt;AggregateEventEntity&gt;&gt;&gt;&gt; GetMultipleAggregateEventRelationshipsAsync&lt;T&gt;(
    ///     List&lt;IAggregateId&lt;T&gt;&gt; aggregateIds, 
    ///     CancellationToken cancellationToken = default) where T : IAggregate, new()
    /// {
    ///     var results = new Dictionary&lt;string, List&lt;AggregateEventEntity&gt;&gt;();
    ///     var failures = new List&lt;string&gt;();
    /// 
    ///     foreach (var aggregateId in aggregateIds)
    ///     {
    ///         try
    ///         {
    ///             var relationshipsResult = await _context.GetAggregateEventEntities(aggregateId, cancellationToken);
    ///             if (relationshipsResult.IsSuccess)
    ///             {
    ///                 results[aggregateId.Id] = relationshipsResult.Value!;
    ///             }
    ///             else
    ///             {
    ///                 failures.Add($"{aggregateId.Id}: {relationshipsResult.Failure!.Description}");
    ///             }
    ///         }
    ///         catch (OperationCanceledException)
    ///         {
    ///             break; // Stop processing on cancellation
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             failures.Add($"{aggregateId.Id}: {ex.Message}");
    ///         }
    ///     }
    /// 
    ///     if (failures.Any() && results.Count == 0)
    ///     {
    ///         return new Failure("Failed to retrieve any aggregate event relationships", 
    ///             string.Join("; ", failures));
    ///     }
    /// 
    ///     return results;
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<List<AggregateEventEntity>>> GetAggregateEventEntities<TAggregate>(this IDomainDbContext domainDbContext, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            return new Failure
            (
                Title: "Aggregate type not found",
                Description: $"Aggregate {typeof(TAggregate).Name} does not have an AggregateType attribute."
            );
        }

        var aggregateEventEntities = await domainDbContext.AggregateEvents.Include(entity => entity.Event).AsNoTracking()
            .Where(entity => entity.AggregateId == aggregateId.ToIdWithTypeVersion(aggregateType.Version))
            .ToListAsync(cancellationToken);

        return aggregateEventEntities.ToList();
    }
}
