using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves the latest event sequence number for a specified stream, with optional filtering by event types.
    /// This method provides the current position in an event stream, essential for optimistic concurrency control
    /// and determining where to append new events in event sourcing operations.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to the event store entities and querying capabilities
    /// for retrieving sequence information from the underlying database.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream whose latest sequence number should be retrieved.
    /// Used to filter events to only those belonging to the specified stream.
    /// </param>
    /// <param name="eventTypeFilter">
    /// An optional array of event types to filter the sequence calculation. When provided, only events
    /// matching the specified types are considered when determining the latest sequence number.
    /// If null or empty, all events in the stream are considered.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the latest
    /// sequence number in the stream, or 0 if the stream has no events or no events match the filter criteria.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get latest sequence for entire stream
    /// public async Task&lt;int&gt; GetCurrentStreamPositionAsync(IStreamId streamId)
    /// {
    ///     var latestSequence = await _context.GetLatestEventSequence(streamId);
    ///     return latestSequence; // Returns total event count in stream
    /// }
    /// 
    /// // Get latest sequence with event type filtering
    /// public async Task&lt;int&gt; GetLatestOrderEventSequenceAsync(IStreamId streamId)
    /// {
    ///     var orderEventTypes = new[] 
    ///     { 
    ///         typeof(OrderCreatedEvent), 
    ///         typeof(OrderUpdatedEvent), 
    ///         typeof(OrderCompletedEvent) 
    ///     };
    ///     
    ///     var sequence = await _context.GetLatestEventSequence(streamId, orderEventTypes);
    ///     return sequence; // Returns count of only order-related events
    /// }
    /// 
    /// // Usage in save operations for concurrency control
    /// public async Task&lt;Result&gt; SaveAggregateWithConcurrencyCheckAsync&lt;T&gt;(
    ///     T aggregate, 
    ///     IStreamId streamId, 
    ///     IAggregateId aggregateId) where T : IAggregate
    /// {
    ///     // Get current sequence for concurrency control
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     // Save with expected sequence to prevent conflicts
    ///     return await _context.Save(streamId, aggregateId, aggregate, currentSequence);
    /// }
    /// 
    /// // Monitoring stream growth over time
    /// public async Task&lt;Dictionary&lt;string, int&gt;&gt; GetStreamSizesAsync(List&lt;IStreamId&gt; streamIds)
    /// {
    ///     var streamSizes = new Dictionary&lt;string, int&gt;();
    ///     
    ///     foreach (var streamId in streamIds)
    ///     {
    ///         var sequence = await _context.GetLatestEventSequence(streamId);
    ///         streamSizes[streamId.Id] = sequence;
    ///     }
    ///     
    ///     return streamSizes;
    /// }
    /// 
    /// // Conditional event processing based on stream state
    /// public async Task&lt;bool&gt; HasNewEventsAsync(IStreamId streamId, int lastProcessedSequence)
    /// {
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     return currentSequence &gt; lastProcessedSequence;
    /// }
    /// 
    /// // Stream synchronization across different systems
    /// public async Task&lt;StreamSyncStatus&gt; GetStreamSyncStatusAsync(
    ///     IStreamId localStreamId, 
    ///     int remoteSequence)
    /// {
    ///     var localSequence = await _context.GetLatestEventSequence(localStreamId);
    ///     
    ///     return localSequence switch
    ///     {
    ///         var seq when seq == remoteSequence =&gt; StreamSyncStatus.InSync,
    ///         var seq when seq &lt; remoteSequence =&gt; StreamSyncStatus.Behind,
    ///         _ =&gt; StreamSyncStatus.Ahead
    ///     };
    /// }
    /// </code>
    /// </example>
    public static async Task<int> GetLatestEventSequence(this IDomainDbContext domainDbContext, IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            return await domainDbContext.Events.AsNoTracking()
                .Where(eventEntity => eventEntity.StreamId == streamId.Id)
                .MaxAsync(eventEntity => (int?)eventEntity.Sequence, cancellationToken) ?? 0;
        }

        var domainEventTypeKeys = eventTypeFilter!
            .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
            .Select(b => b.Key).ToList();

        return await domainDbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.StreamId == streamId.Id && domainEventTypeKeys.Contains($"{eventEntity.TypeName}:{eventEntity.TypeVersion}"))
            .MaxAsync(eventEntity => (int?)eventEntity.Sequence, cancellationToken) ?? 0;
    }
}
