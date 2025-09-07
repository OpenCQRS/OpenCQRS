using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves domain events starting from a specified sequence number, optionally filtered by event types.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream to which the domain events belong.</param>
    /// <param name="fromSequence">The sequence number from which to start retrieving domain events.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the retrieved domain events. If null, no filtering is applied.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved domain events wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<List<IDomainEvent>>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
}
