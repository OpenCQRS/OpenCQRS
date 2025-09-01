using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDomainService : IDomainService
{
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ICosmosDataStore _cosmosDataStore;

    public CosmosDomainService(ICosmosClientConnection cosmosClientConnection, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor, ICosmosDataStore cosmosDataStore)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _cosmosClient = new CosmosClient(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
        _container = _cosmosClient.GetContainer(cosmosClientConnection.DatabaseName, cosmosClientConnection.ContainerName);
        _cosmosDataStore = cosmosDataStore;
    }

    public async Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateDocumentResult = await _cosmosDataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
        if (aggregateDocumentResult.IsNotSuccess)
        {
            return aggregateDocumentResult.Failure!;
        }

        if (aggregateDocumentResult.Value != null)
        {
            var currentAggregate = aggregateDocumentResult.Value.ToAggregate<TAggregate>();

            if (!applyNewDomainEvents)
            {
                return currentAggregate;
            }

            return await UpdateAggregate(streamId, aggregateId, cancellationToken);
        }

        var aggregate = new TAggregate();

        var aggregateType = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateType == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }

        var eventDocumentsResult = await _cosmosDataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!.ToList();
        if (eventDocuments.Count == 0)
        {
            return aggregate;
        }

        var domainEvents = eventDocuments.Select(eventDoc => eventDoc.ToDomainEvent()).ToList();
        aggregate.Apply(domainEvents);
        if (aggregate.Version == 0)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = eventDocuments.OrderBy(eventDoc => eventDoc.Sequence).Last().Sequence;
        var aggregateDocument = aggregate.ToAggregateDocument(streamId, aggregateId, latestEventSequenceForAggregate);

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            aggregateDocument.CreatedDate = timeStamp;
            aggregateDocument.CreatedBy = currentUserNameIdentifier;
            aggregateDocument.UpdatedDate = timeStamp;
            aggregateDocument.UpdatedBy = currentUserNameIdentifier;
            batch.CreateItem(aggregateDocument);

            foreach (var eventDocument in eventDocuments)
            {
                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToIdWithTypeVersion(aggregateType.Version)}:{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToIdWithTypeVersion(aggregateType.Version),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                batch.CreateItem(aggregateEventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            if (batchResponse.IsSuccessStatusCode)
            {
                return aggregate;
            }

            var tags = new Dictionary<string, object> { { "Message", batchResponse.ErrorMessage } };
            Activity.Current?.AddEvent(new ActivityEvent("Batch execution failed when creating the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error creating the aggregate",
                Description: "There was an error when creating the aggregate"
            );
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when creating the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error creating the aggregate",
                Description: "There was an error when creating the aggregate"
            );
        }
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocuments(streamId, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        throw new NotImplementedException();
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocumentsFromSequence(streamId, fromSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocumentsUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }

    public async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            return new Failure
            (
                Title: "Aggregate type not found",
                Description: $"Aggregate {typeof(TAggregate).Name} does not have an AggregateType attribute."
            );
        }

        var aggregate = new TAggregate();

        var eventDocumentsResult = upToSequence > 0
            ? await _cosmosDataStore.GetEventDocumentsUpToSequence(streamId, upToSequence.Value, aggregate.EventTypeFilter, cancellationToken)
            : await _cosmosDataStore.GetEventDocuments(streamId, aggregate.EventTypeFilter, cancellationToken);

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
        aggregate.AggregateId = aggregateId.ToIdWithTypeVersion(aggregateType.Version);
        aggregate.LatestEventSequence = eventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        aggregate.Apply(eventDocuments.Select(eventEntity => eventEntity.ToDomainEvent()));

        return aggregate;
    }

    public async Task<Result<int>> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;
        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT VALUE MAX(c.sequence) FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT VALUE MAX(c.sequence) FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType)";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }

        try
        {
            using var iterator = _container.GetItemQueryIterator<int?>(queryDefinition);

            if (!iterator.HasMoreResults)
            {
                return 0;
            }

            var response = await iterator.ReadNextAsync(cancellationToken);
            var result = response.FirstOrDefault();
            return result ?? 0;
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the latest event sequence", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the latest event sequence",
                Description: "There was an error when retrieving the latest event sequence"
            );
        }
    }

    public async Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        if (!aggregate.UncommittedEvents.Any())
        {
            return Result.Ok();
        }

        var latestEventSequenceResult = await GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
        if (latestEventSequenceResult.IsNotSuccess)
        {
            return latestEventSequenceResult.Failure!;
        }
        var latestEventSequence = latestEventSequenceResult.Value;

        if (latestEventSequence != expectedEventSequence)
        {
            return new Failure
            (
                Title: "Concurrency exception",
                Description: $"Expected event sequence {expectedEventSequence} but found {latestEventSequence}"
            );
        }

        var aggregateType = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateType == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }

        var newLatestEventSequenceForAggregate = latestEventSequence + aggregate.UncommittedEvents.Count();
        var currentAggregateVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
        var aggregateIsNew = currentAggregateVersion == 0;

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            var aggregateDocument = aggregate.ToAggregateDocument(streamId, aggregateId, newLatestEventSequenceForAggregate);
            aggregateDocument.UpdatedDate = timeStamp;
            aggregateDocument.UpdatedBy = currentUserNameIdentifier;
            if (aggregateIsNew)
            {
                aggregateDocument.CreatedDate = timeStamp;
                aggregateDocument.CreatedBy = currentUserNameIdentifier;
            }
            else
            {
                var existingAggregateDocumentResult = await _cosmosDataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
                if (existingAggregateDocumentResult.IsNotSuccess)
                {
                    return existingAggregateDocumentResult.Failure!;
                }
                var existingAggregateDocument = existingAggregateDocumentResult.Value;
                if (existingAggregateDocument != null)
                {
                    aggregateDocument.CreatedDate = existingAggregateDocument.CreatedDate;
                    aggregateDocument.CreatedBy = existingAggregateDocument.CreatedBy;
                }
                else
                {
                    aggregateDocument.CreatedDate = timeStamp;
                    aggregateDocument.CreatedBy = currentUserNameIdentifier;
                }
            }
            batch.UpsertItem(aggregateDocument);

            foreach (var @event in aggregate.UncommittedEvents)
            {
                var eventDocument = @event.ToEventDocument(streamId, sequence: ++latestEventSequence);
                eventDocument.CreatedDate = timeStamp;
                eventDocument.CreatedBy = currentUserNameIdentifier;
                batch.CreateItem(eventDocument);

                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToIdWithTypeVersion(aggregateType.Version)}:{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToIdWithTypeVersion(aggregateType.Version),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                batch.CreateItem(aggregateEventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            if (batchResponse.IsSuccessStatusCode)
            {
                return Result.Ok();
            }

            var tags = new Dictionary<string, object> { { "Message", batchResponse.ErrorMessage } };
            Activity.Current?.AddEvent(new ActivityEvent("Batch execution failed when saving the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving the aggregate",
                Description: "There was an error when saving the aggregate"
            );
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when saving the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving the aggregate",
                Description: "There was an error when saving the aggregate"
            );
        }
    }

    public async Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        if (domainEvents.Length == 0)
        {
            return Result.Ok();
        }

        var latestEventSequenceResult = await GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
        if (latestEventSequenceResult.IsNotSuccess)
        {
            return latestEventSequenceResult.Failure!;
        }
        var latestEventSequence = latestEventSequenceResult.Value;
        if (latestEventSequence != expectedEventSequence)
        {
            return new Failure
            (
                Title: "Concurrency exception",
                Description: $"Expected event sequence {expectedEventSequence} but found {latestEventSequence}"
            );
        }

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            foreach (var @event in domainEvents)
            {
                var eventDocument = @event.ToEventDocument(streamId, sequence: ++latestEventSequence);
                eventDocument.CreatedDate = timeStamp;
                eventDocument.CreatedBy = currentUserNameIdentifier;
                batch.CreateItem(eventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            if (batchResponse.IsSuccessStatusCode)
            {
                return Result.Ok();
            }

            var tags = new Dictionary<string, object> { { "Message", batchResponse.ErrorMessage } };
            Activity.Current?.AddEvent(new ActivityEvent("Batch execution failed when saving the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving the aggregate",
                Description: "There was an error when saving the aggregate"
            );
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when saving the domain events", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving the domain events",
                Description: "There was an error when saving the domain events"
            );
        }
    }

    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            return new Failure
            (
                Title: "Aggregate type not found",
                Description: $"Aggregate {typeof(TAggregate).Name} does not have an AggregateType attribute."
            );
        }

        var aggregateDocumentResult = await _cosmosDataStore.GetAggregateDocument(streamId, aggregateId, cancellationToken);
        if (aggregateDocumentResult.IsNotSuccess)
        {
            return aggregateDocumentResult.Failure!;
        }
        var aggregateDocument = aggregateDocumentResult.Value;
        if (aggregateDocument is null)
        {
            return new TAggregate();
        }

        return await _cosmosDataStore.UpdateAggregate(streamId, aggregateId, aggregateDocument, cancellationToken);
    }

    public void Dispose() => _cosmosClient.Dispose();
}
