using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves the domain events associated with the specified stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream for which domain events are to be retrieved.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the domain events. If null, no filtering is applied.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a result wrapping a list of domain events.</returns>
    Task<Result<List<IDomainEvent>>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default);
}
