using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    public static async Task<List<IDomainEvent>> GetDomainEventsUpToDate(this IDomainDbContext domainDbContext, IStreamId streamId, DateTimeOffset upToDate, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesUpToDate(streamId, upToDate, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
