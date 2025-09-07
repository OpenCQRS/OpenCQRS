using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.DomainService;

public partial interface IDomainService
{
    Task<Result<List<IDomainEvent>>> GetDomainEventsBetweenSequences(
        IStreamId streamId,
        int fromSequence,
        int toSequence,
        Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default);
}
