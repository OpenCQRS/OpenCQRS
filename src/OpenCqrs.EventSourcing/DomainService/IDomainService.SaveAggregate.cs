using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Saves the specified aggregate to the event stream.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate to be saved.</typeparam>
    /// <param name="streamId">The unique identifier of the event stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate to save.</param>
    /// <param name="aggregate">The aggregate instance containing the state to persist.</param>
    /// <param name="expectedEventSequence">The sequence number of the last known event, used for optimistic concurrency control.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains a <see cref="Result"/> indicating the outcome of the operation.</returns>
    Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId,
        TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default)
        where TAggregate : IAggregate, new();
}
