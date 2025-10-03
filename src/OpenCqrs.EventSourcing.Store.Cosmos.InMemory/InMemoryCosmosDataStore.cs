using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.InMemory;

public class InMemoryCosmosDataStore : ICosmosDataStore
{
    public Task<Result<AggregateDocument?>> GetAggregateDocument<T>(IStreamId streamId, IAggregateId<T> aggregateId,
        CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<AggregateEventDocument>>> GetAggregateEventDocuments<T>(IStreamId streamId, IAggregateId<T> aggregateId,
        CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, string[] eventIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsBetweenSequences(IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsUpToDate(IStreamId streamId, DateTimeOffset upToDate, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsFromDate(IStreamId streamId, DateTimeOffset fromDate, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<List<EventDocument>>> GetEventDocumentsBetweenDates(IStreamId streamId, DateTimeOffset fromDate, DateTimeOffset toDate,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<T?>> UpdateAggregateDocument<T>(IStreamId streamId, IAggregateId<T> aggregateId, AggregateDocument? aggregateDocument,
        CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
