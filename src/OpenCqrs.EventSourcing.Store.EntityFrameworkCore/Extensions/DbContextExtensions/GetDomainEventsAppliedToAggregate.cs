using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves all domain events that have been applied to a specific aggregate instance.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="aggregateId">The unique identifier for the aggregate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the list of applied domain events or a failure.</returns>
    /// <example>
    /// <code>
    /// var result = await context.GetDomainEventsAppliedToAggregate(aggregateId);
    /// if (!result.IsSuccess)
    /// {
    ///     return result.Failure;
    /// }
    /// var events = result.Value;
    /// </code>
    /// </example>
    public static async Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(this IDomainDbContext domainDbContext, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var eventEntitiesAppliedToAggregate = await domainDbContext.GetEventEntitiesAppliedToAggregate(aggregateId, cancellationToken);
        if (eventEntitiesAppliedToAggregate.IsNotSuccess)
        {
            return eventEntitiesAppliedToAggregate.Failure!;
        }

        return eventEntitiesAppliedToAggregate.Value!.Select(entity => entity.ToDomainEvent()).ToList();
    }
}
