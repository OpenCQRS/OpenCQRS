using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    public static async Task<List<IDomainEvent>> GetDomainEventsBetweenSequences(this IDomainDbContext domainDbContext, IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesBetweenSequences(streamId, fromSequence, toSequence, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
