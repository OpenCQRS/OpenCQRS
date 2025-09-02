using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves a list of event entities from the specified stream starting from a given sequence number.
    /// </summary>
    /// <param name="domainDbContext">The domain database context to query event entities from.</param>
    /// <param name="streamId">The stream identifier to filter events by.</param>
    /// <param name="fromSequence">The starting sequence number (inclusive) from which to retrieve events.</param>
    /// <param name="eventTypeFilter">Optional array of event types to filter by. If null or empty, all event types are included.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of event entities ordered by sequence.</returns>
    /// <example>
    /// <code>
    /// // Get all events from sequence 10 onwards
    /// var allEvents = await context.GetEventEntitiesFromSequence(streamId, 10);
    /// 
    /// // Get only specific event types from sequence 5 onwards
    /// var eventTypes = new Type[] { typeof(OrderCreated), typeof(OrderUpdated) };
    /// var filteredEvents = await context.GetEventEntitiesFromSequence(streamId, 5, eventTypes);
    /// 
    /// // Get events from latest sequence for aggregate replay
    /// var latestSequence = aggregate.LatestEventSequence + 1;
    /// var newEvents = await context.GetEventEntitiesFromSequence(streamId, latestSequence, aggregate.EventTypeFilter);
    /// </code>
    /// </example>
    public static async Task<List<EventEntity>> GetEventEntitiesFromSequence(this IDomainDbContext domainDbContext, IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            return await domainDbContext.Events.AsNoTracking()
                .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence >= fromSequence)
                .OrderBy(eventEntity => eventEntity.Sequence)
                .ToListAsync(cancellationToken);
        }

        var eventTypes = eventTypeFilter!
            .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
            .Select(b => b.Key).ToList();

        return await domainDbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence >= fromSequence && eventTypes.Contains(eventEntity.EventType))
            .OrderBy(eventEntity => eventEntity.Sequence)
            .ToListAsync(cancellationToken);
    }
}
