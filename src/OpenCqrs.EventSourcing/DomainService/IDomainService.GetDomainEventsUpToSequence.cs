using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves a list of domain events up to a specified sequence number from a given stream,
    /// optionally filtered by event types.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream containing the domain events.</param>
    /// <param name="upToSequence">The sequence number up to which events should be retrieved.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the domain events.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of domain events wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<List<IDomainEvent>>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
}
