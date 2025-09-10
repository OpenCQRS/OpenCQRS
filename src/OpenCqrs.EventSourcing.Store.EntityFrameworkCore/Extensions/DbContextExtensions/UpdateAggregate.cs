using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Updates an existing aggregate with new events from its stream.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="domainDbContext">The domain database context.</param>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="aggregateId">The unique identifier for the aggregate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the updated aggregate or a failure.</returns>
    /// <example>
    /// <code>
    /// var result = await context.UpdateAggregate(streamId, aggregateId);
    /// if (!result.IsSuccess)
    /// {
    ///     return result.Failure;
    /// }
    /// var aggregate = result.Value;
    /// </code>
    /// </example>
    public static async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregateRoot, new()
    {
        var aggregateEntity = await domainDbContext.Aggregates.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == aggregateId.ToStoreId(), cancellationToken);
        if (aggregateEntity is null)
        {
            return new TAggregate();
        }

        var aggregate = aggregateEntity.ToAggregate<TAggregate>();

        return await domainDbContext.UpdateAggregate(streamId, aggregateId, aggregate, cancellationToken);
    }
}
