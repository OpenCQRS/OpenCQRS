using OpenCqrs.EventSourcing.Data;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Tracks an aggregate's uncommitted events and state changes in the Entity Framework change tracker
    /// without persisting to the database, preparing all necessary entities for subsequent save operations.
    /// This method provides comprehensive tracking with concurrency control validation and relationship management.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate being tracked. Must implement <see cref="IAggregate"/> to provide
    /// access to uncommitted events, version information, and aggregate state.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides change tracking capabilities and access to event
    /// store entities for preparing persistence operations.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream where the aggregate's events will be stored,
    /// ensuring proper event ordering and stream isolation.
    /// </param>
    /// <param name="aggregateId">
    /// The unique identifier for the aggregate being tracked, used for creating aggregate snapshots
    /// and establishing relationships between aggregates and their events.
    /// </param>
    /// <param name="aggregate">
    /// The aggregate instance containing uncommitted events and current state that needs to be
    /// tracked for persistence, including business logic changes and domain events.
    /// </param>
    /// <param name="expectedEventSequence">
    /// The expected sequence number of the last event in the stream, used for optimistic concurrency
    /// control to prevent lost updates in multi-user scenarios.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a tuple with tracked entities:
    /// <list type="bullet">
    /// <item><description><see cref="List{EventEntity}"/>?: The event entities created from uncommitted events, null if no events</description></item>
    /// <item><description><see cref="AggregateEntity"/>?: The aggregate snapshot entity, null if no changes</description></item>
    /// <item><description><see cref="List{AggregateEventEntity}"/>?: The relationship entities linking aggregate to events, null if no events</description></item>
    /// </list>
    /// On failure, returns a <see cref="Failure"/> with concurrency violation or validation error details.
    /// </returns>
    /// <example>
    /// <code>
    /// // Manual tracking for batch operations
    /// public async Task&lt;Result&gt; ProcessOrderBatchAsync(List&lt;ProcessOrderRequest&gt; requests)
    /// {
    ///     var trackedEntities = new List&lt;(List&lt;EventEntity&gt;?, AggregateEntity?, List&lt;AggregateEventEntity&gt;?)&gt;();
    ///     
    ///     foreach (var request in requests)
    ///     {
    ///         var streamId = new StreamId($"order-{request.OrderId}");
    ///         var aggregateId = new OrderAggregateId(request.OrderId);
    ///         
    ///         // Get current aggregate
    ///         var aggregateResult = await _context.GetAggregate(streamId, aggregateId);
    ///         if (aggregateResult.IsNotSuccess) continue;
    ///         
    ///         var order = aggregateResult.Value!;
    ///         var expectedSequence = await _context.GetLatestEventSequence(streamId);
    ///         
    ///         // Apply business logic
    ///         order.ProcessPayment(request.PaymentInfo);
    ///         order.UpdateStatus(OrderStatus.Processing);
    ///         
    ///         // Track changes without saving
    ///         var trackResult = await _context.Track(streamId, aggregateId, order, expectedSequence);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///             
    ///         trackedEntities.Add(trackResult.Value!);
    ///     }
    ///     
    ///     // Save all tracked changes at once
    ///     return await _context.Save();
    /// }
    /// 
    /// // Custom persistence workflow
    /// public class CustomPersistenceHandler : IRequestHandler&lt;CustomPersistenceRequest&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; Handle(CustomPersistenceRequest request, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId($"custom-{request.EntityId}");
    ///         var aggregateId = new CustomAggregateId(request.EntityId);
    ///         var expectedSequence = request.ExpectedVersion;
    ///         
    ///         // Create and modify aggregate
    ///         var aggregate = new CustomAggregate(request.EntityId);
    ///         aggregate.ApplyBusinessRules(request.BusinessData);
    ///         aggregate.ValidateState();
    ///         
    ///         // Track changes for review
    ///         var trackResult = await _context.Track(streamId, aggregateId, aggregate, expectedSequence, cancellationToken);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var (events, aggregateEntity, relationships) = trackResult.Value!;
    ///         
    ///         // Custom validation of tracked changes
    ///         if (events?.Count &gt; 10)
    ///         {
    ///             return new Failure("Too many events", "Cannot process more than 10 events in single operation");
    ///         }
    ///         
    ///         // Conditional persistence based on business rules
    ///         if (request.RequiresApproval && !request.IsApproved)
    ///         {
    ///             // Store for later approval without persisting
    ///             await StoreForApproval(events, aggregateEntity, relationships);
    ///             return Result.Ok();
    ///         }
    ///         
    ///         // Proceed with normal persistence
    ///         return await _context.Save(cancellationToken);
    ///     }
    /// }
    /// 
    /// // Multi-aggregate transaction handling
    /// public async Task&lt;Result&gt; ProcessMultiAggregateTransactionAsync(
    ///     Dictionary&lt;IStreamId, (IAggregateId, IAggregate, int)&gt; aggregates)
    /// {
    ///     var allTrackedEntities = new List&lt;object&gt;();
    ///     
    ///     foreach (var kvp in aggregates)
    ///     {
    ///         var streamId = kvp.Key;
    ///         var (aggregateId, aggregate, expectedSequence) = kvp.Value;
    ///         
    ///         var trackResult = await _context.Track(streamId, aggregateId, aggregate, expectedSequence);
    ///         if (trackResult.IsNotSuccess)
    ///         {
    ///             // Rollback all tracking if any aggregate fails
    ///             _context.ChangeTracker.Clear();
    ///             return trackResult.Failure!;
    ///         }
    ///         
    ///         allTrackedEntities.Add(trackResult.Value!);
    ///     }
    ///     
    ///     // All aggregates tracked successfully, now save
    ///     return await _context.Save();
    /// }
    /// 
    /// // Event sourcing with custom conflict resolution
    /// public async Task&lt;Result&gt; SaveWithConflictResolutionAsync&lt;T&gt;(
    ///     IStreamId streamId,
    ///     IAggregateId aggregateId, 
    ///     T aggregate,
    ///     int expectedSequence,
    ///     Func&lt;T, List&lt;IDomainEvent&gt;, T&gt; conflictResolver) where T : IAggregate
    /// {
    ///     var trackResult = await _context.Track(streamId, aggregateId, aggregate, expectedSequence);
    ///     
    ///     if (trackResult.IsNotSuccess)
    ///     {
    ///         // Check if it's a concurrency issue
    ///         if (trackResult.Failure!.Title.Contains("Concurrency"))
    ///         {
    ///             // Get conflicting events and resolve
    ///             var conflictingEvents = await _context.GetDomainEventsFromSequence(streamId, expectedSequence + 1);
    ///             var resolvedAggregate = conflictResolver(aggregate, conflictingEvents);
    ///             
    ///             // Retry with resolved aggregate
    ///             var newSequence = await _context.GetLatestEventSequence(streamId);
    ///             return await SaveWithConflictResolutionAsync(streamId, aggregateId, resolvedAggregate, newSequence, conflictResolver);
    ///         }
    ///         
    ///         return trackResult.Failure!;
    ///     }
    ///     
    ///     return await _context.Save();
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<(List<EventEntity>? EventEntities, AggregateEntity? AggregateEntity, List<AggregateEventEntityForEfCore>? AggregateEventEntities)>> TrackAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
        if (!aggregate.UncommittedEvents.Any())
        {
            return (null, null, null);
        }

        var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
        if (latestEventSequence != expectedEventSequence)
        {
            return new Failure
            (
                Title: "Concurrency exception",
                Description: $"Expected event sequence {expectedEventSequence} but found {latestEventSequence}"
            );
        }

        var newLatestEventSequenceForAggregate = latestEventSequence + aggregate.UncommittedEvents.Count();
        var currentAggregateVersion = aggregate.Version - aggregate.UncommittedEvents.Count();

        var trackedEventEntities = domainDbContext.TrackEventEntities(streamId, aggregate.UncommittedEvents.ToArray(), startingEventSequence: latestEventSequence + 1);
        var trackedAggregateEntity = domainDbContext.TrackAggregateEntity(streamId, aggregateId, aggregate, newLatestEventSequenceForAggregate, aggregateIsNew: currentAggregateVersion == 0);
        var trackedAggregateEventEntities = domainDbContext.TrackAggregateEventEntities(trackedAggregateEntity, trackedEventEntities);

        return (trackedEventEntities, trackedAggregateEntity, trackedAggregateEventEntities);
    }
}
