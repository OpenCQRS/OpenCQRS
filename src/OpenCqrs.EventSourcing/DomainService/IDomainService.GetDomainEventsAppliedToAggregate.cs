using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves the list of domain events that were applied to the specified aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate for which to retrieve applied domain events.</typeparam>
    /// <param name="streamId">The unique identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate whose applied domain events are to be retrieved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of applied domain events wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IStreamId streamId,
        IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : IAggregate, new();
}
