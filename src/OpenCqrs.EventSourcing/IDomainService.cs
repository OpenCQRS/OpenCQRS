using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing;

public interface IDomainService : IDisposable
{
    Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result<List<IDomainEvent>>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result<List<IDomainEvent>>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<List<IDomainEvent>>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result<int>> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();

    // TODO: GetDomainEventsBetweenSequences (Issue #124)
    // TODO: GetDomainEventsUpToDate (Issue #124)
    // TODO: GetDomainEventsFromDate (Issue #124)
    // TODO: GetDomainEventsBetweenDates (Issue #124)

    // TODO: GetDomainEvents as stream (Issue #122)
    // TODO: GetDomainEventsUpToSequence as stream (Issue #122)
    // TODO: GetDomainEventsFromSequence as stream (Issue #122)
    // TODO: GetDomainEventsBetweenSequences as stream (Issue #122)
    // TODO: GetDomainEventsUpToDate as stream (Issue #122)
    // TODO: GetDomainEventsFromDate as stream (Issue #122)
    // TODO: GetDomainEventsBetweenDates as stream (Issue #122)

    // TODO: GetInMemoryAggregateUpToSequence (Issue #122)
    // TODO: GetInMemoryAggregateUpToDate (Issue #122)
}
