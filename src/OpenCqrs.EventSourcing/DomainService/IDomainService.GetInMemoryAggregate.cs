using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves an in-memory aggregate of the specified type up to a specified sequence, if provided.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate to retrieve.</typeparam>
    /// <param name="streamId">The unique identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate to retrieve.</param>
    /// <param name="upToSequence">The sequence number up to which events are included. If null, all events are included.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the aggregate wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId,
        int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
}
