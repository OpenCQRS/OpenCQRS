using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public interface ICosmosDataStore : IDisposable
{
    Task<Result<AggregateDocument?>> GetAggregateDocument<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result<List<AggregateEventDocument>>> GetAggregateEventDocuments<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();
    Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<List<EventDocument>>> GetEventDocumentsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<List<EventDocument>>> GetEventDocumentsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, AggregateDocument aggregateDocument, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();

    // TODO: GetEventDocumentsBetweenSequences (Issue #124)
    // TODO: GetEventDocumentsUpToDate (Issue #124)
    // TODO: GetEventDocumentsFromDate (Issue #124)
    // TODO: GetEventDocumentsBetweenDates (Issue #124)

    // TODO: GetEventDocuments as stream (Issue #122)
    // TODO: GetEventDocumentsUpToSequence as stream (Issue #122)
    // TODO: GetEventDocumentsFromSequence as stream (Issue #122)
    // TODO: GetEventDocumentsBetweenSequences as stream (Issue #122)
    // TODO: GetEventDocumentsUpToDate as stream (Issue #122)
    // TODO: GetEventDocumentsFromDate as stream (Issue #122)
    // TODO: GetEventDocumentsBetweenDates as stream (Issue #122)
}
