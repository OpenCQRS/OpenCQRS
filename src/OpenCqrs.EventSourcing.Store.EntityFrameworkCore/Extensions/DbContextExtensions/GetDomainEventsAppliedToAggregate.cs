using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all domain events that have been applied to a specific aggregate instance,
    /// using the explicit aggregate-event relationship tracking. This method provides precise
    /// access to the events that actually contributed to an aggregate's current state.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate whose applied events should be retrieved. Must implement
    /// <see cref="IAggregate"/> and have the <see cref="AggregateType"/> attribute for proper
    /// type resolution and versioned identifier generation.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to aggregate-event relationships
    /// and the underlying event entities for retrieval and deserialization operations.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate whose applied events should be retrieved.
    /// Used to locate the specific aggregate-event relationships in the database.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either a list of <see cref="IDomainEvent"/> objects
    /// representing all events that were applied to the specified aggregate, or a <see cref="Failure"/>
    /// if the aggregate type lacks required metadata or other retrieval issues occur.
    /// </returns>
    /// <example>
    /// <code>
    /// // Generate audit trail for specific aggregate
    /// public async Task&lt;AggregateAuditTrail&gt; GenerateAuditTrailAsync(Guid orderId)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var eventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     if (eventsResult.IsNotSuccess)
    ///     {
    ///         _logger.LogError("Failed to retrieve events for audit trail: {Error}",
    ///             eventsResult.Failure?.Description);
    ///         return new AggregateAuditTrail { OrderId = orderId, Error = eventsResult.Failure?.Description };
    ///     }
    ///     
    ///     var appliedEvents = eventsResult.Value!;
    ///     
    ///     return new AggregateAuditTrail
    ///     {
    ///         OrderId = orderId,
    ///         TotalEventsApplied = appliedEvents.Count,
    ///         EventSummary = appliedEvents
    ///             .GroupBy(e =&gt; e.GetType().Name)
    ///             .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         FirstEvent = appliedEvents.FirstOrDefault(),
    ///         LastEvent = appliedEvents.LastOrDefault(),
    ///         EventTimeline = appliedEvents
    ///             .Select(e =&gt; new EventAuditEntry
    ///             {
    ///                 EventType = e.GetType().Name,
    ///                 OccurredAt = e.OccurredAt,
    ///                 Sequence = e.Sequence,
    ///                 Description = GenerateEventDescription(e)
    ///             })
    ///             .OrderBy(entry =&gt; entry.Sequence)
    ///             .ToList()
    ///     };
    /// }
    /// 
    /// // Verify aggregate state against applied events
    /// public async Task&lt;AggregateVerificationResult&gt; VerifyAggregateConsistencyAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Get current aggregate state
    ///     var currentAggregateResult = await _context.GetAggregate(streamId, aggregateId);
    ///     if (currentAggregateResult.IsNotSuccess)
    ///     {
    ///         return new AggregateVerificationResult 
    ///         { 
    ///             IsConsistent = false, 
    ///             Error = "Failed to load current aggregate state" 
    ///         };
    ///     }
    ///     
    ///     // Get applied events
    ///     var appliedEventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     if (appliedEventsResult.IsNotSuccess)
    ///     {
    ///         return new AggregateVerificationResult 
    ///         { 
    ///             IsConsistent = false, 
    ///             Error = "Failed to retrieve applied events" 
    ///         };
    ///     }
    ///     
    ///     // Reconstruct aggregate from applied events
    ///     var reconstructedAggregate = new OrderAggregate();
    ///     reconstructedAggregate.Apply(appliedEventsResult.Value!);
    ///     
    ///     // Compare states
    ///     var currentAggregate = currentAggregateResult.Value!;
    ///     var isConsistent = CompareAggregateStates(currentAggregate, reconstructedAggregate);
    ///     
    ///     return new AggregateVerificationResult
    ///     {
    ///         IsConsistent = isConsistent,
    ///         CurrentVersion = currentAggregate.Version,
    ///         ReconstructedVersion = reconstructedAggregate.Version,
    ///         AppliedEventCount = appliedEventsResult.Value!.Count,
    ///         Differences = isConsistent ? new List&lt;string&gt;() : FindStateDifferences(currentAggregate, reconstructedAggregate)
    ///     };
    /// }
    /// 
    /// // Impact analysis: Find events that led to specific state
    /// public async Task&lt;EventImpactAnalysis&gt; AnalyzeImpactOnAggregateAsync(
    ///     Guid orderId, 
    ///     Func&lt;IDomainEvent, bool&gt; eventFilter)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     var eventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     
    ///     if (eventsResult.IsNotSuccess)
    ///     {
    ///         return new EventImpactAnalysis 
    ///         { 
    ///             OrderId = orderId, 
    ///             Error = eventsResult.Failure?.Description 
    ///         };
    ///     }
    ///     
    ///     var allAppliedEvents = eventsResult.Value!;
    ///     var relevantEvents = allAppliedEvents.Where(eventFilter).ToList();
    ///     
    ///     // Analyze state before and after relevant events
    ///     var stateProgression = new List&lt;StateAnalysisPoint&gt;();
    ///     var workingAggregate = new OrderAggregate();
    ///     
    ///     foreach (var appliedEvent in allAppliedEvents.OrderBy(e =&gt; e.Sequence))
    ///     {
    ///         var isRelevantEvent = relevantEvents.Contains(appliedEvent);
    ///         
    ///         if (isRelevantEvent)
    ///         {
    ///             // Capture state before applying this event
    ///             stateProgression.Add(new StateAnalysisPoint
    ///             {
    ///                 Sequence = appliedEvent.Sequence,
    ///                 EventType = appliedEvent.GetType().Name,
    ///                 IsRelevantEvent = true,
    ///                 StateBefore = CaptureAggregateState(workingAggregate)
    ///             });
    ///         }
    ///         
    ///         workingAggregate.Apply(new[] { appliedEvent });
    ///         
    ///         if (isRelevantEvent)
    ///         {
    ///             // Capture state after applying this event
    ///             stateProgression.Last().StateAfter = CaptureAggregateState(workingAggregate);
    ///         }
    ///     }
    ///     
    ///     return new EventImpactAnalysis
    ///     {
    ///         OrderId = orderId,
    ///         TotalAppliedEvents = allAppliedEvents.Count,
    ///         RelevantEventCount = relevantEvents.Count,
    ///         StateProgression = stateProgression,
    ///         FinalState = CaptureAggregateState(workingAggregate)
    ///     };
    /// }
    /// 
    /// // Compliance report with event-level details
    /// public async Task&lt;ComplianceReport&gt; GenerateComplianceReportAsync(
    ///     Guid orderId, 
    ///     DateTime reportingPeriodStart,
    ///     DateTime reportingPeriodEnd)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     var eventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     
    ///     if (eventsResult.IsNotSuccess)
    ///     {
    ///         return new ComplianceReport 
    ///         { 
    ///             OrderId = orderId, 
    ///             Status = ComplianceStatus.Error,
    ///             ErrorMessage = eventsResult.Failure?.Description 
    ///         };
    ///     }
    ///     
    ///     var appliedEvents = eventsResult.Value!;
    ///     var reportingPeriodEvents = appliedEvents
    ///         .Where(e =&gt; e.OccurredAt &gt;= reportingPeriodStart && e.OccurredAt &lt;= reportingPeriodEnd)
    ///         .OrderBy(e =&gt; e.OccurredAt)
    ///         .ToList();
    ///     
    ///     return new ComplianceReport
    ///     {
    ///         OrderId = orderId,
    ///         ReportingPeriod = (reportingPeriodStart, reportingPeriodEnd),
    ///         Status = ComplianceStatus.Complete,
    ///         TotalEventsInPeriod = reportingPeriodEvents.Count,
    ///         EventsByType = reportingPeriodEvents
    ///             .GroupBy(e =&gt; e.GetType().Name)
    ///             .ToDictionary(g =&gt; g.Key, g =&gt; g.Count()),
    ///         ComplianceEvents = reportingPeriodEvents
    ///             .Where(e =&gt; IsComplianceRelevantEvent(e))
    ///             .Select(e =&gt; new ComplianceEventEntry
    ///             {
    ///                 EventType = e.GetType().Name,
    ///                 OccurredAt = e.OccurredAt,
    ///                 Description = GenerateComplianceDescription(e),
    ///                 RegulatoryCategory = GetRegulatoryCategory(e)
    ///             })
    ///             .ToList()
    ///     };
    /// }
    /// 
    /// // Debugging: Trace aggregate evolution through applied events
    /// public async Task&lt;List&lt;AggregateEvolutionStep&gt;&gt; TraceAggregateEvolutionAsync(Guid orderId)
    /// {
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     var eventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     
    ///     if (eventsResult.IsNotSuccess)
    ///     {
    ///         _logger.LogError("Failed to retrieve applied events for tracing: {Error}",
    ///             eventsResult.Failure?.Description);
    ///         return new List&lt;AggregateEvolutionStep&gt;();
    ///     }
    ///     
    ///     var appliedEvents = eventsResult.Value!.OrderBy(e =&gt; e.Sequence).ToList();
    ///     var evolutionSteps = new List&lt;AggregateEvolutionStep&gt;();
    ///     var workingAggregate = new OrderAggregate();
    ///     
    ///     // Add initial state
    ///     evolutionSteps.Add(new AggregateEvolutionStep
    ///     {
    ///         StepNumber = 0,
    ///         EventType = "Initial State",
    ///         AggregateVersion = 0,
    ///         StateSnapshot = CaptureAggregateState(workingAggregate)
    ///     });
    ///     
    ///     // Trace through each applied event
    ///     for (int i = 0; i &lt; appliedEvents.Count; i++)
    ///     {
    ///         var domainEvent = appliedEvents[i];
    ///         workingAggregate.Apply(new[] { domainEvent });
    ///         
    ///         evolutionSteps.Add(new AggregateEvolutionStep
    ///         {
    ///             StepNumber = i + 1,
    ///             EventType = domainEvent.GetType().Name,
    ///             EventSequence = domainEvent.Sequence,
    ///             EventOccurredAt = domainEvent.OccurredAt,
    ///             AggregateVersion = workingAggregate.Version,
    ///             StateSnapshot = CaptureAggregateState(workingAggregate),
    ///             EventData = domainEvent
    ///         });
    ///     }
    ///     
    ///     _logger.LogInformation("Traced aggregate evolution for order {OrderId} through {StepCount} steps",
    ///         orderId, evolutionSteps.Count - 1);
    ///     
    ///     return evolutionSteps;
    /// }
    /// 
    /// // Performance comparison: Applied vs Stream events
    /// public async Task&lt;EventRetrievalComparison&gt; CompareRetrievalMethodsAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Time applied events retrieval
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var appliedEventsResult = await _context.GetDomainEventsAppliedToAggregate(aggregateId);
    ///     stopwatch.Stop();
    ///     var appliedEventsTime = stopwatch.Elapsed;
    ///     
    ///     // Time stream events retrieval
    ///     stopwatch.Restart();
    ///     var streamEvents = await _context.GetDomainEvents(streamId);
    ///     stopwatch.Stop();
    ///     var streamEventsTime = stopwatch.Elapsed;
    ///     
    ///     return new EventRetrievalComparison
    ///     {
    ///         OrderId = orderId,
    ///         AppliedEventsCount = appliedEventsResult.IsSuccess ? appliedEventsResult.Value!.Count : 0,
    ///         StreamEventsCount = streamEvents.Count,
    ///         AppliedEventsRetrievalTime = appliedEventsTime,
    ///         StreamEventsRetrievalTime = streamEventsTime,
    ///         RetrievalTimeRatio = streamEventsTime.TotalMilliseconds / appliedEventsTime.TotalMilliseconds,
    ///         FilteringEfficiency = appliedEventsResult.IsSuccess 
    ///             ? (double)appliedEventsResult.Value!.Count / streamEvents.Count
    ///             : 0.0
    ///     };
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(this IDomainDbContext domainDbContext, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var eventEntitiesAppliedToAggregate = await domainDbContext.GetEventEntitiesAppliedToAggregate(aggregateId, cancellationToken);
        if (eventEntitiesAppliedToAggregate.IsNotSuccess)
        {
            return eventEntitiesAppliedToAggregate.Failure!;
        }

        return eventEntitiesAppliedToAggregate.Value!.Select(entity => entity.ToDomainEvent()).ToList();
    }
}
