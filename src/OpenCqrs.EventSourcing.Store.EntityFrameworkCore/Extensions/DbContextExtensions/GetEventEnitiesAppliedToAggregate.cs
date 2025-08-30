using OpenCqrs.EventSourcing.Data;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all event entities that have been applied to a specific aggregate instance, providing
    /// a complete audit trail of changes that contributed to the aggregate's current state. This method
    /// enables aggregate event history analysis, debugging, and compliance reporting by returning the
    /// exact events used in aggregate construction and state transitions.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate whose applied events should be retrieved. Must implement <see cref="IAggregate"/>,
    /// have a parameterless constructor for aggregate reconstruction, and be decorated with the
    /// <see cref="AggregateType"/> attribute for proper type metadata extraction.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to aggregate-event relationship entities
    /// and their associated event data through Entity Framework Core navigation properties.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate whose applied events should be retrieved.
    /// This identifier is combined with the aggregate type version to create a versioned lookup key
    /// that ensures accurate event retrieval across aggregate type evolution scenarios.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous database operation if needed,
    /// supporting graceful shutdown scenarios and preventing resource leaks during long-running queries.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either a list of <see cref="EventEntity"/> instances
    /// representing all events that were applied during the aggregate's construction and evolution,
    /// or a <see cref="Failure"/> if the operation failed due to missing aggregate type metadata
    /// or if no events were found for the specified aggregate identifier.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the aggregate type does not have the required <see cref="AggregateType"/> attribute.
    /// This attribute is essential for creating the versioned aggregate identifier used in event lookups.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via the provided <paramref name="cancellationToken"/>,
    /// typically during application shutdown or when operation timeouts are exceeded.
    /// </exception>
    /// <example>
    /// <code>
    /// // Retrieve events applied to an order aggregate for audit purposes
    /// public async Task&lt;Result&lt;AuditReport&gt;&gt; GenerateOrderAuditAsync(Guid orderId)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var eventsResult = await _context.GetEventEntitiesAppliedToAggregate(aggregateId);
    ///     if (eventsResult.IsNotSuccess)
    ///         return eventsResult.Failure!;
    ///         
    ///     var events = eventsResult.Value!;
    ///     
    ///     return new AuditReport
    ///     {
    ///         AggregateId = orderId,
    ///         EventCount = events.Count,
    ///         FirstEventDate = events.FirstOrDefault()?.CreatedDate,
    ///         LastEventDate = events.LastOrDefault()?.CreatedDate,
    ///         EventTypes = events.Select(e =&gt; $"{e.TypeName} v{e.TypeVersion}").Distinct().ToList(),
    ///         EventDetails = events.Select(e =&gt; new AuditEventDetail
    ///         {
    ///             Sequence = e.Sequence,
    ///             EventType = e.TypeName,
    ///             Version = e.TypeVersion,
    ///             CreatedAt = e.CreatedDate,
    ///             Data = e.Data
    ///         }).ToList()
    ///     };
    /// }
    /// 
    /// // Debug aggregate state by examining applied events
    /// public async Task&lt;Result&gt; DebugAggregateStateAsync&lt;T&gt;(IAggregateId&lt;T&gt; aggregateId) 
    ///     where T : IAggregate, new()
    /// {
    ///     var eventsResult = await _context.GetEventEntitiesAppliedToAggregate(aggregateId);
    ///     if (eventsResult.IsNotSuccess)
    ///     {
    ///         _logger.LogError("Failed to retrieve events for aggregate {AggregateId}: {Error}", 
    ///             aggregateId.Id, eventsResult.Failure!.Description);
    ///         return eventsResult.Failure!;
    ///     }
    ///     
    ///     var events = eventsResult.Value!;
    ///     
    ///     _logger.LogInformation("Aggregate {AggregateId} built from {EventCount} events:", 
    ///         aggregateId.Id, events.Count);
    ///         
    ///     foreach (var eventEntity in events.OrderBy(e =&gt; e.Sequence))
    ///     {
    ///         _logger.LogInformation("  Sequence {Sequence}: {EventType} v{Version} at {Timestamp}", 
    ///             eventEntity.Sequence, eventEntity.TypeName, eventEntity.TypeVersion, eventEntity.CreatedDate);
    ///     }
    ///     
    ///     return Result.Ok();
    /// }
    /// 
    /// // Compliance reporting with event history
    /// public async Task&lt;Result&lt;ComplianceReport&gt;&gt; GenerateComplianceReportAsync(
    ///     List&lt;Guid&gt; aggregateIds, 
    ///     DateTimeOffset fromDate, 
    ///     DateTimeOffset toDate)
    /// {
    ///     var reportEvents = new List&lt;ComplianceEventRecord&gt;();
    ///     
    ///     foreach (var id in aggregateIds)
    ///     {
    ///         var aggregateId = new OrderAggregateId(id);
    ///         var eventsResult = await _context.GetEventEntitiesAppliedToAggregate(aggregateId);
    ///         
    ///         if (eventsResult.IsSuccess)
    ///         {
    ///             var filteredEvents = eventsResult.Value!
    ///                 .Where(e =&gt; e.CreatedDate &gt;= fromDate &amp;&amp; e.CreatedDate &lt;= toDate)
    ///                 .ToList();
    ///                 
    ///             reportEvents.AddRange(filteredEvents.Select(e =&gt; new ComplianceEventRecord
    ///             {
    ///                 AggregateId = id,
    ///                 EventType = e.TypeName,
    ///                 EventVersion = e.TypeVersion,
    ///                 OccurredAt = e.CreatedDate,
    ///                 Sequence = e.Sequence,
    ///                 EventData = e.Data
    ///             }));
    ///         }
    ///     }
    ///     
    ///     return new ComplianceReport
    ///     {
    ///         ReportPeriod = new DateRange(fromDate, toDate),
    ///         AggregateCount = aggregateIds.Count,
    ///         TotalEvents = reportEvents.Count,
    ///         EventsByType = reportEvents.GroupBy(e =&gt; e.EventType)
    ///             .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         Events = reportEvents.OrderBy(e =&gt; e.OccurredAt).ToList()
    ///     };
    /// }
    /// 
    /// // Event replay verification
    /// public async Task&lt;Result&lt;bool&gt;&gt; VerifyAggregateStateAsync&lt;T&gt;(IAggregateId&lt;T&gt; aggregateId) 
    ///     where T : IAggregate, new()
    /// {
    ///     // Get current aggregate from snapshot
    ///     var currentResult = await _context.GetAggregate(new StreamId($"stream-{aggregateId.Id}"), aggregateId);
    ///     if (currentResult.IsNotSuccess)
    ///         return currentResult.Failure!;
    ///         
    ///     var currentAggregate = currentResult.Value!;
    ///     
    ///     // Get all applied events
    ///     var eventsResult = await _context.GetEventEntitiesAppliedToAggregate(aggregateId);
    ///     if (eventsResult.IsNotSuccess)
    ///         return eventsResult.Failure!;
    ///         
    ///     var appliedEvents = eventsResult.Value!;
    ///     
    ///     // Rebuild aggregate from events
    ///     var rebuiltAggregate = new T();
    ///     rebuiltAggregate.FromEventStore(appliedEvents.OrderBy(e =&gt; e.Sequence).ToList());
    ///     
    ///     // Compare states
    ///     var statesMatch = JsonConvert.SerializeObject(currentAggregate) == 
    ///                      JsonConvert.SerializeObject(rebuiltAggregate);
    ///                      
    ///     if (!statesMatch)
    ///     {
    ///         _logger.LogWarning("Aggregate {AggregateId} state mismatch detected", aggregateId.Id);
    ///     }
    ///     
    ///     return statesMatch;
    /// }
    /// 
    /// // Performance monitoring of event application
    /// public async Task&lt;Result&lt;PerformanceMetrics&gt;&gt; AnalyzeAggregatePerformanceAsync&lt;T&gt;(
    ///     IAggregateId&lt;T&gt; aggregateId) where T : IAggregate, new()
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    ///     
    ///     var eventsResult = await _context.GetEventEntitiesAppliedToAggregate(aggregateId);
    ///     if (eventsResult.IsNotSuccess)
    ///         return eventsResult.Failure!;
    ///         
    ///     var queryTime = stopwatch.ElapsedMilliseconds;
    ///     var events = eventsResult.Value!;
    ///     
    ///     stopwatch.Restart();
    ///     var aggregate = new T();
    ///     aggregate.FromEventStore(events);
    ///     var replayTime = stopwatch.ElapsedMilliseconds;
    ///     
    ///     return new PerformanceMetrics
    ///     {
    ///         AggregateId = aggregateId.Id,
    ///         EventCount = events.Count,
    ///         QueryTimeMs = queryTime,
    ///         ReplayTimeMs = replayTime,
    ///         TotalTimeMs = queryTime + replayTime,
    ///         AverageEventSize = events.Average(e =&gt; e.Data?.Length ?? 0),
    ///         EventTimeSpan = events.Any() 
    ///             ? events.Max(e =&gt; e.CreatedDate) - events.Min(e =&gt; e.CreatedDate)
    ///             : TimeSpan.Zero
    ///     };
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<List<EventEntity>>> GetEventEntitiesAppliedToAggregate<TAggregate>(this IDomainDbContext domainDbContext, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateEventEntitiesResult = await domainDbContext.GetAggregateEventEntities(aggregateId, cancellationToken);
        if (aggregateEventEntitiesResult.IsNotSuccess)
        {
            return aggregateEventEntitiesResult.Failure!;
        }

        return aggregateEventEntitiesResult.Value!.Select(entity => entity.Event).ToList();
    }
}
