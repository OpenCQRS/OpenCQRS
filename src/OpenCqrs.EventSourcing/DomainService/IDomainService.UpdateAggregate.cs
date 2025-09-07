using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Updates an aggregate of the specified type.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate to update.</typeparam>
    /// <param name="streamId">The unique identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated aggregate wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId,
        CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
}
