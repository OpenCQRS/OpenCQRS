using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    /// <summary>
    /// Saves the specified domain events to the underlying event store.
    /// </summary>
    /// <param name="streamId">The unique identifier representing the stream to which the domain events belong.</param>
    /// <param name="domainEvents">An array of domain events to save.</param>
    /// <param name="expectedEventSequence">The expected sequence of events in the stream, ensuring concurrency control.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result is a <see cref="Result"/> indicating success or failure of the operation.</returns>
    Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence,
        CancellationToken cancellationToken = default);
}
