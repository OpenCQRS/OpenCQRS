using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public interface ICosmosDataStore : IDisposable
{
    /// <summary>
    /// Retrieves an aggregate document from the Cosmos data store.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="streamId">The identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing the retrieved aggregate document or a failure.</returns>
    Task<Result<AggregateDocument?>> GetAggregateDocument<TAggregate>(IStreamId streamId,
        IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : IAggregate, new();

    /// <summary>
    /// Retrieves the event documents associated with an aggregate from the Cosmos data store.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="streamId">The identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing the retrieved list of aggregate event documents or a failure.</returns>
    Task<Result<List<AggregateEventDocument>>> GetAggregateEventDocuments<TAggregate>(IStreamId streamId,
        IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default)
        where TAggregate : IAggregate, new();

    /// <summary>
    /// Retrieves a list of event documents from the Cosmos data store for a specific stream.
    /// </summary>
    /// <param name="streamId">The identifier of the stream from which the events are to be retrieved.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the results. If null, all event types will be retrieved.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing the list of event documents or a failure.</returns>
    Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves event documents from the Cosmos data store based on a stream and a set of event identifiers.
    /// </summary>
    /// <param name="streamId">The identifier of the stream to which the events belong.</param>
    /// <param name="eventIds">An array of event identifiers to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing a list of retrieved event documents or a failure.</returns>
    Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, string[] eventIds,
        CancellationToken cancellationToken = default);

    Task<Result<List<EventDocument>>> GetEventDocumentsBetweenSequences(IStreamId streamId, int fromSequence,
        int toSequence, Type[]? eventTypeFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of event documents from the specified sequence in the Cosmos data store.
    /// </summary>
    /// <param name="streamId">The identifier of the stream for which to retrieve event documents.</param>
    /// <param name="fromSequence">The sequence number from which to start retrieving event documents.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the results. If null, all event types are included.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation of the operation.</param>
    /// <returns>A result containing a list of retrieved event documents or a failure.</returns>
    Task<Result<List<EventDocument>>> GetEventDocumentsFromSequence(IStreamId streamId, int fromSequence,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves event documents from the Cosmos data store up to a specified sequence number.
    /// </summary>
    /// <param name="streamId">The identifier of the stream from which the event documents are retrieved.</param>
    /// <param name="upToSequence">The maximum sequence number up to which the event documents should be retrieved.</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the retrieved documents.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing a list of event documents retrieved or a failure.</returns>
    Task<Result<List<EventDocument>>> GetEventDocumentsUpToSequence(IStreamId streamId, int upToSequence,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate document in the Cosmos data store.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="streamId">The identifier of the stream to which the aggregate belongs.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="aggregateDocument">The aggregate document containing updated data.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result containing the updated aggregate or a failure.</returns>
    Task<Result<TAggregate>> UpdateAggregateDocument<TAggregate>(IStreamId streamId,
        IAggregateId<TAggregate> aggregateId, AggregateDocument aggregateDocument,
        CancellationToken cancellationToken = default) where TAggregate : IAggregate, new();

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
