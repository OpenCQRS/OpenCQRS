using Microsoft.AspNetCore.Http;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.InMemory;

public class InMemoryCosmosDomainService(
    InMemoryCosmosStorage storage,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : IDomainService
{
    private readonly InMemoryCosmosDataStore _dataStore = new(storage);

    public async Task<Result<T?>> GetAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, ReadMode readMode = ReadMode.SnapshotOnly, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        var aggregateDocumentResult = await _dataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
        if (aggregateDocumentResult.IsNotSuccess)
        {
            return aggregateDocumentResult.Failure!;
        }

        if (aggregateDocumentResult.Value != null)
        {
            var currentAggregateDocument = aggregateDocumentResult.Value;
            switch (readMode)
            {
                case ReadMode.SnapshotOnly or ReadMode.SnapshotOrCreate:
                    return currentAggregateDocument.ToAggregate<T>();
                case ReadMode.SnapshotWithNewEvents or ReadMode.SnapshotWithNewEventsOrCreate:
                    return await _dataStore.UpdateAggregateDocument(streamId, aggregateId, currentAggregateDocument, cancellationToken);
            }
        }

        if (readMode is ReadMode.SnapshotOnly or ReadMode.SnapshotWithNewEvents)
        {
            return default(T);
        }

        var aggregate = new T();

        var eventDocumentsResult = await _dataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return default(T);
        }

        var events = eventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
        aggregate.Apply(events);
        if (aggregate.Version == 0)
        {
            return default(T);
        }

        var timeStamp = timeProvider.GetUtcNow();
        var currentUserNameIdentifier = httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var latestEventSequenceForAggregate = eventDocuments.OrderBy(eventDocument => eventDocument.Sequence).Last().Sequence;
            var aggregateDocument = aggregate.ToAggregateDocument(streamId, aggregateId, latestEventSequenceForAggregate);
            aggregateDocument.CreatedDate = timeStamp;
            aggregateDocument.CreatedBy = currentUserNameIdentifier;
            aggregateDocument.UpdatedDate = timeStamp;
            aggregateDocument.UpdatedBy = currentUserNameIdentifier;
            
            var key = InMemoryCosmosStorage.CreateAggregateKey(streamId, aggregateId);
            var aggregateAdded = storage.AggregateDocuments.TryAdd(key, aggregateDocument);
            if (!aggregateAdded)
            {
                throw new Exception("Could not add aggregate");
            }

            foreach (var eventDocument in eventDocuments)
            {
                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToStoreId()}|{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToStoreId(),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                
                if (!storage.AggregateEventDocuments.TryGetValue(key, out var bag))
                {
                    bag = [];
                    storage.AggregateEventDocuments.TryAdd(key, bag);
                }
                bag.Add(aggregateEventDocument);
            }
            
            return aggregate;
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operation: "Get Aggregate");
            return ErrorHandling.DefaultFailure;
        }
    }

    public async Task<Result<List<IEvent>>> GetEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocuments(streamId, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsAppliedToAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        var aggregateEventDocumentsResult = await _dataStore.GetAggregateEventDocuments(streamId, aggregateId, cancellationToken);
        if (aggregateEventDocumentsResult.IsNotSuccess)
        {
            return aggregateEventDocumentsResult.Failure!;
        }
        var aggregateEventDocuments = aggregateEventDocumentsResult.Value!;
        if (aggregateEventDocuments.Count == 0)
        {
            return new List<IEvent>();
        }

        var eventDocumentsResult = await _dataStore.GetEventDocuments(streamId, aggregateEventDocuments.Select(ae => ae.EventId).ToArray(), cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!;
        return eventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsBetweenSequences(IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsBetweenSequences(streamId, fromSequence, toSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsFromSequence(streamId, fromSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsUpToDate(IStreamId streamId, DateTimeOffset upToDate, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsUpToDate(streamId, upToDate, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsFromDate(IStreamId streamId, DateTimeOffset fromDate, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsFromDate(streamId, fromDate, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IEvent>>> GetEventsBetweenDates(IStreamId streamId, DateTimeOffset fromDate, DateTimeOffset toDate, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _dataStore.GetEventDocumentsBetweenDates(streamId, fromDate, toDate, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<T>> GetInMemoryAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        var aggregate = new T();

        var eventDocumentsResult = await _dataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventDocuments.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public async Task<Result<T>> GetInMemoryAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, int upToSequence, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        var aggregate = new T();

        var eventDocumentsResult = await _dataStore.GetEventDocumentsUpToSequence(streamId, upToSequence, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventDocuments.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public async Task<Result<T>> GetInMemoryAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, DateTimeOffset upToDate, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        var aggregate = new T();

        var eventDocumentsResult = await _dataStore.GetEventDocumentsUpToDate(streamId, upToDate, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToStoreId();
        aggregate.LatestEventSequence = eventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventDocuments.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public Task<Result<int>> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> SaveAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, T aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        throw new NotImplementedException();
    }

    public Task<Result> SaveEvents(IStreamId streamId, IEvent[] events, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<T?>> UpdateAggregate<T>(IStreamId streamId, IAggregateId<T> aggregateId, CancellationToken cancellationToken = default) where T : IAggregateRoot, new()
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
