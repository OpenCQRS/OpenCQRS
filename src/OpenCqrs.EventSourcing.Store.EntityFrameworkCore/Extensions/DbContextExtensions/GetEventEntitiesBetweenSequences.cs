using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    public static async Task<List<EventEntity>> GetEventEntitiesBetweenSequences(this IDomainDbContext domainDbContext, IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            return await domainDbContext.Events.AsNoTracking()
                .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence >= fromSequence && eventEntity.Sequence <= toSequence)
                .OrderBy(eventEntity => eventEntity.Sequence)
                .ToListAsync(cancellationToken);
        }

        var eventTypes = eventTypeFilter!
            .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
            .Select(b => b.Key).ToList();

        return await domainDbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.StreamId == streamId.Id && eventEntity.Sequence >= fromSequence && eventEntity.Sequence <= toSequence && eventTypes.Contains(eventEntity.EventType))
            .OrderBy(eventEntity => eventEntity.Sequence)
            .ToListAsync(cancellationToken);
    }
}
