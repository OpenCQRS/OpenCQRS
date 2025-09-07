using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Retrieves the latest event sequence number for a specific stream.
    /// </summary>
    /// <param name="streamId">The unique identifier of the stream for which the latest event sequence is being retrieved.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the events to be considered when determining the latest sequence number.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the latest event sequence number wrapped in a <see cref="Result{TValue}"/>.</returns>
    Task<Result<int>> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default);
}
