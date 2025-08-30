using OpenCqrs.EventSourcing.Data;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Tracks an array of domain events in the Entity Framework change tracker without persisting
    /// to the database, preparing event entities for later save operations with proper
    /// sequencing and concurrency control validation.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides change tracking capabilities and access to
    /// event store entities for preparing persistence operations.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream where these events will be stored, ensuring
    /// proper event ordering and stream isolation across different bounded contexts.
    /// </param>
    /// <param name="domainEvents">
    /// An array of domain events implementing <see cref="IDomainEvent"/> to be tracked for
    /// persistence. Each event will be converted to an <see cref="EventEntity"/> with proper
    /// serialization and metadata.
    /// </param>
    /// <param name="expectedEventSequence">
    /// The expected sequence number of the last event currently in the stream, used for optimistic
    /// concurrency control. New events will be sequenced starting from this value + 1.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A list of <see cref="EventEntity"/> containing either the list of tracked event entities
    /// ready for persistence, or a <see cref="Failure"/> if concurrency validation fails or other
    /// tracking issues occur.
    /// </returns>
    /// <example>
    /// <code>
    /// // Batch event tracking with conditional persistence
    /// public async Task&lt;Result&gt; ProcessEventBatchAsync(List&lt;IDomainEvent[]&gt; eventBatches)
    /// {
    ///     var allTrackedEvents = new List&lt;EventEntity&gt;();
    ///     var streamId = new StreamId("batch-processing-stream");
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     foreach (var batch in eventBatches)
    ///     {
    ///         var trackResult = await _context.Track(streamId, batch, currentSequence);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         allTrackedEvents.AddRange(trackResult.Value!);
    ///         currentSequence += batch.Length;
    ///     }
    ///     
    ///     // Validate all tracked events before persisting
    ///     if (await ValidateEventBatch(allTrackedEvents))
    ///     {
    ///         return await _context.Save();
    ///     }
    ///     
    ///     // Clear tracking if validation fails
    ///     _context.ChangeTracker.Clear();
    ///     return new Failure("Validation failed", "Event batch validation did not pass");
    /// }
    /// 
    /// // Event staging for approval workflows
    /// public class EventApprovalHandler : IRequestHandler&lt;StageEventsForApprovalRequest&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&lt;string&gt;&gt; Handle(StageEventsForApprovalRequest request, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId($"approval-{request.RequestId}");
    ///         var expectedSequence = 0; // New approval stream
    ///         
    ///         // Track events without persisting
    ///         var trackResult = await _context.Track(streamId, request.EventsToApprove, expectedSequence, cancellationToken);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var trackedEvents = trackResult.Value!;
    ///         
    ///         // Store tracked events in approval staging area
    ///         var stagingId = await StoreInApprovalQueue(trackedEvents, request.RequesterUserId);
    ///         
    ///         // Clear tracking since we're not persisting yet
    ///         _context.ChangeTracker.Clear();
    ///         
    ///         return stagingId;
    ///     }
    ///     
    ///     public async Task&lt;Result&gt; ApproveEventsAsync(string stagingId, string approverId)
    ///     {
    ///         var stagedEvents = await RetrieveFromApprovalQueue(stagingId);
    ///         var streamId = ExtractStreamIdFromStaging(stagingId);
    ///         var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///         
    ///         // Convert back to domain events for re-tracking
    ///         var domainEvents = stagedEvents.Select(e =&gt; e.ToDomainEvent()).ToArray();
    ///         
    ///         // Track and persist approved events
    ///         var trackResult = await _context.Track(streamId, domainEvents, currentSequence);
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         return await _context.Save();
    ///     }
    /// }
    /// 
    /// // Multi-stream event coordination
    /// public async Task&lt;Result&gt; CoordinateAcrossStreamsAsync(
    ///     Dictionary&lt;IStreamId, IDomainEvent[]&gt; streamEventMap)
    /// {
    ///     var allTrackedEvents = new Dictionary&lt;IStreamId, List&lt;EventEntity&gt;&gt;();
    ///     
    ///     // Track events in all streams first
    ///     foreach (var kvp in streamEventMap)
    ///     {
    ///         var streamId = kvp.Key;
    ///         var events = kvp.Value;
    ///         var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///         
    ///         var trackResult = await _context.Track(streamId, events, currentSequence);
    ///         if (trackResult.IsNotSuccess)
    ///         {
    ///             // Rollback all tracking on any failure
    ///             _context.ChangeTracker.Clear();
    ///             return trackResult.Failure!;
    ///         }
    ///         
    ///         allTrackedEvents[streamId] = trackResult.Value!;
    ///     }
    ///     
    ///     // Validate cross-stream consistency
    ///     if (await ValidateCrossStreamConsistency(allTrackedEvents))
    ///     {
    ///         return await _context.Save();
    ///     }
    ///     
    ///     _context.ChangeTracker.Clear();
    ///     return new Failure("Coordination failed", "Cross-stream consistency validation failed");
    /// }
    /// 
    /// // Event transformation and re-tracking
    /// public class EventTransformationService
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; TransformAndReplayEventsAsync(
    ///         IStreamId sourceStreamId,
    ///         IStreamId targetStreamId,
    ///         Func&lt;IDomainEvent, IDomainEvent&gt; transformer)
    ///     {
    ///         // Get source events
    ///         var sourceEvents = await _context.GetDomainEvents(sourceStreamId);
    ///         
    ///         // Transform events
    ///         var transformedEvents = sourceEvents.Select(transformer).ToArray();
    ///         
    ///         // Track transformed events in target stream
    ///         var targetSequence = await _context.GetLatestEventSequence(targetStreamId);
    ///         var trackResult = await _context.Track(targetStreamId, transformedEvents, targetSequence);
    ///         
    ///         if (trackResult.IsNotSuccess)
    ///             return trackResult.Failure!;
    ///         
    ///         var trackedEvents = trackResult.Value!;
    ///         
    ///         // Add transformation metadata
    ///         foreach (var eventEntity in trackedEvents)
    ///         {
    ///             eventEntity.Metadata = eventEntity.Metadata ?? new Dictionary&lt;string, object&gt;();
    ///             eventEntity.Metadata["TransformedFrom"] = sourceStreamId.Id;
    ///             eventEntity.Metadata["TransformationTimestamp"] = DateTime.UtcNow;
    ///         }
    ///         
    ///         return await _context.Save();
    ///     }
    /// }
    /// 
    /// // Performance-optimized bulk event tracking
    /// public async Task&lt;Result&gt; BulkTrackEventsAsync(
    ///     IStreamId streamId,
    ///     IEnumerable&lt;IDomainEvent[]&gt; eventChunks,
    ///     int batchSize = 1000)
    /// {
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     var processedCount = 0;
    ///     
    ///     foreach (var chunk in eventChunks.Chunk(batchSize))
    ///     {
    ///         foreach (var events in chunk)
    ///         {
    ///             var trackResult = await _context.Track(streamId, events, currentSequence);
    ///             if (trackResult.IsNotSuccess)
    ///                 return trackResult.Failure!;
    ///             
    ///             currentSequence += events.Length;
    ///             processedCount += events.Length;
    ///         }
    ///         
    ///         // Persist batch
    ///         var saveResult = await _context.Save();
    ///         if (saveResult.IsNotSuccess)
    ///             return saveResult;
    ///         
    ///         // Clear change tracker for memory efficiency
    ///         _context.ChangeTracker.Clear();
    ///     }
    ///     
    ///     return Result.Ok();
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<List<EventEntity>>> TrackDomainEvents(this IDomainDbContext domainDbContext, IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        if (domainEvents.Length == 0)
        {
            return new List<EventEntity>();
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

        var trackedEntities = domainDbContext.TrackEventEntities(streamId, domainEvents, startingEventSequence: latestEventSequence + 1);

        return trackedEntities;
    }
}
