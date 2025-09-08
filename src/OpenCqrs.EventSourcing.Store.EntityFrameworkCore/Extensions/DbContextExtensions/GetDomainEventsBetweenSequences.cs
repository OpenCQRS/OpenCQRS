using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Gets domain events between two specific sequence numbers with optional event type filtering.
    /// </summary>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="fromSequence">The starting sequence number (inclusive).</param>
    /// <param name="toSequence">The ending sequence number (inclusive).</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the retrieved domain events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of domain events between the specified sequence numbers.</returns>
    public static async Task<List<IDomainEvent>> GetDomainEventsBetweenSequences(this IDomainDbContext domainDbContext, IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesBetweenSequences(streamId, fromSequence, toSequence, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
