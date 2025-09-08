using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Reconstructs an aggregate entirely from events without using snapshots, providing a pure event-sourced
    /// view of the aggregate state. This method bypasses snapshot storage and builds the aggregate by applying
    /// all relevant events from the event stream, optionally up to a specific sequence number.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate to reconstruct. Must implement <see cref="IAggregate"/> and have a parameterless
    /// constructor to support event-based reconstruction from a clean state.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event entities and querying capabilities
    /// for retrieving events needed for aggregate reconstruction.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream containing the events that define the aggregate's history.
    /// All events in this stream matching the aggregate's event filter will be considered for application.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate being reconstructed. Used to set the
    /// aggregate's identity properties during the reconstruction process.
    /// </param>
    /// <param name="upToSequence">
    /// An optional sequence number limit for event application. When specified, only events with
    /// sequence numbers less than or equal to this value will be applied. If null, all available
    /// events will be applied to achieve the most current state.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{TAggregate}"/> containing either the successfully reconstructed aggregate
    /// with its state built from events, or a <see cref="Failure"/> if the aggregate type lacks
    /// required metadata or other reconstruction issues occur.
    /// </returns>
    /// <example>
    /// <code>
    /// // Basic in-memory aggregate reconstruction
    /// public async Task&lt;OrderAggregate&gt; GetOrderHistoryAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var result = await _context.GetInMemoryAggregate(streamId, aggregateId);
    ///     return result.IsSuccess ? result.Value! : new OrderAggregate();
    /// }
    /// 
    /// // Time-travel: Get aggregate state at specific point in time
    /// public async Task&lt;OrderAggregate&gt; GetOrderStateAtSequenceAsync(Guid orderId, int sequence)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var result = await _context.GetInMemoryAggregate(streamId, aggregateId, upToSequence: sequence);
    ///     return result.IsSuccess ? result.Value! : new OrderAggregate();
    /// }
    /// 
    /// // Snapshot verification against event history
    /// public async Task&lt;bool&gt; VerifySnapshotAccuracyAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Get aggregate from snapshot
    ///     var snapshotResult = await _context.GetAggregate(streamId, aggregateId, applyNewDomainEvents: false);
    ///     if (snapshotResult.IsNotSuccess || snapshotResult.Value!.Version == 0)
    ///         return true; // No snapshot to verify
    ///     
    ///     // Reconstruct from events up to the snapshot's sequence
    ///     var reconstructedResult = await _context.GetInMemoryAggregate(
    ///         streamId, aggregateId, upToSequence: snapshotResult.Value.LatestEventSequence);
    ///     
    ///     if (reconstructedResult.IsNotSuccess)
    ///         return false;
    ///     
    ///     // Compare key properties
    ///     var snapshot = snapshotResult.Value;
    ///     var reconstructed = reconstructedResult.Value!;
    ///     
    ///     return snapshot.Version == reconstructed.Version &&
    ///            snapshot.GetBusinessState().Equals(reconstructed.GetBusinessState());
    /// }
    /// 
    /// // Historical analysis: Track how aggregate changed over time
    /// public async Task&lt;List&lt;OrderStateSnapshot&gt;&gt; GetOrderHistorySnapshotsAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var snapshots = new List&lt;OrderStateSnapshot&gt;();
    ///     var totalEvents = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     // Create snapshots at key intervals
    ///     for (int sequence = 1; sequence &lt;= totalEvents; sequence += 5)
    ///     {
    ///         var result = await _context.GetInMemoryAggregate(
    ///             streamId, aggregateId, upToSequence: sequence);
    ///             
    ///         if (result.IsSuccess && result.Value!.Version &gt; 0)
    ///         {
    ///             snapshots.Add(new OrderStateSnapshot
    ///             {
    ///                 Sequence = sequence,
    ///                 Version = result.Value.Version,
    ///                 Status = result.Value.Status,
    ///                 TotalAmount = result.Value.TotalAmount,
    ///                 ItemCount = result.Value.Items.Count,
    ///                 Timestamp = DateTime.UtcNow // Would typically get from events
    ///             });
    ///         }
    ///     }
    ///     
    ///     return snapshots;
    /// }
    /// 
    /// // Performance comparison: Snapshot vs. full reconstruction
    /// public async Task&lt;TimeSpan&gt; MeasureReconstructionTimeAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var result = await _context.GetInMemoryAggregate(streamId, aggregateId);
    ///     stopwatch.Stop();
    ///     
    ///     _logger.LogInformation("Reconstructed order {OrderId} from {EventCount} events in {Duration}ms",
    ///         orderId, result.Value?.Version ?? 0, stopwatch.ElapsedMilliseconds);
    ///     
    ///     return stopwatch.Elapsed;
    /// }
    /// 
    /// // Testing: Verify aggregate behavior with specific event sequences
    /// public async Task&lt;bool&gt; TestAggregateStateAfterEventsAsync(
    ///     List&lt;IDomainEvent&gt; testEvents, 
    ///     Func&lt;OrderAggregate, bool&gt; stateValidator)
    /// {
    ///     var testStreamId = new StreamId($"test-{Guid.NewGuid()}");
    ///     var testAggregateId = new OrderAggregateId(Guid.NewGuid());
    ///     
    ///     // Save test events to a temporary stream
    ///     var saveResult = await _context.Save(testStreamId, testEvents.ToArray(), 0);
    ///     if (saveResult.IsNotSuccess)
    ///         return false;
    ///     
    ///     try
    ///     {
    ///         // Reconstruct aggregate from test events
    ///         var result = await _context.GetInMemoryAggregate(testStreamId, testAggregateId);
    ///         if (result.IsNotSuccess)
    ///             return false;
    ///         
    ///         // Validate the resulting state
    ///         return stateValidator(result.Value!);
    ///     }
    ///     finally
    ///     {
    ///         // Cleanup test stream (implementation dependent)
    ///         await CleanupTestStream(testStreamId);
    ///     }
    /// }
    /// 
    /// // Compliance: Generate audit report showing state changes
    /// public async Task&lt;AuditReport&gt; GenerateAuditReportAsync(Guid orderId, DateTime fromDate, DateTime toDate)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Get all events in the date range
    ///     var events = await _context.GetDomainEvents(streamId);
    ///     var relevantEvents = events
    ///         .Where(e =&gt; e.OccurredAt &gt;= fromDate && e.OccurredAt &lt;= toDate)
    ///         .ToList();
    ///     
    ///     var report = new AuditReport { OrderId = orderId, FromDate = fromDate, ToDate = toDate };
    ///     
    ///     // Generate state snapshots at key points
    ///     var lastSequence = 0;
    ///     foreach (var eventGroup in relevantEvents.GroupBy(e =&gt; e.OccurredAt.Date))
    ///     {
    ///         var dayEvents = eventGroup.OrderBy(e =&gt; e.Sequence).ToList();
    ///         var endSequence = dayEvents.Last().Sequence;
    ///         
    ///         var stateResult = await _context.GetInMemoryAggregate(
    ///             streamId, aggregateId, upToSequence: endSequence);
    ///             
    ///         if (stateResult.IsSuccess)
    ///         {
    ///             report.DailySnapshots.Add(eventGroup.Key, new AuditSnapshot
    ///             {
    ///                 State = stateResult.Value!,
    ///                 EventsApplied = dayEvents,
    ///                 SequenceRange = (lastSequence + 1, endSequence)
    ///             });
    ///         }
    ///         
    ///         lastSequence = endSequence;
    ///     }
    ///     
    ///     return report;
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventEntities = upToSequence > 0
            ? await domainDbContext.GetEventEntitiesUpToSequence(streamId, upToSequence.Value, aggregate.EventTypeFilter, cancellationToken)
            : await domainDbContext.GetEventEntities(streamId, aggregate.EventTypeFilter, cancellationToken);

        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public static async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, DateTimeOffset upToDate, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntitiesUpToDate(streamId, upToDate, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }
}
