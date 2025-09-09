using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

/// <summary>
/// Provides extension methods for IDomainDbContext.
/// </summary>
public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves domain events up to a specified date for a given stream.
    /// </summary>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="upToDate">The end date.</param>
    /// <param name="eventTypeFilter">Optional filter for event types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of domain events.</returns>
    public static async Task<List<IDomainEvent>> GetDomainEventsUpToDate(this IDomainDbContext domainDbContext, IStreamId streamId, DateTimeOffset upToDate, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventEntities = await domainDbContext.GetEventEntitiesUpToDate(streamId, upToDate, eventTypeFilter, cancellationToken);
        return eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
    }
}
