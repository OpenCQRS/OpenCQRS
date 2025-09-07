using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
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

    public CosmosDomainService(IOptions<CosmosOptions> options, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor, ICosmosDataStore cosmosDataStore)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _cosmosClient = new CosmosClient(options.Value.Endpoint, options.Value.AuthKey, options.Value.ClientOptions);
        _container = _cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
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

        var domainEvents = eventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
        aggregate.Apply(domainEvents);
        if (aggregate.Version == 0)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = eventDocuments.OrderBy(eventDocument => eventDocument.Sequence).Last().Sequence;
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
            batchResponse.AddActivityEvent(streamId, aggregateId, aggregateType);
            return batchResponse.IsSuccessStatusCode
                ? aggregate
                : new Failure
                (
                    Title: "Error",
                    Description: "There was an error when processing the request"
                );
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Get Aggregate");
            return ErrorHandling.DefaultFailure;
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

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateEventDocumentsResult = await _cosmosDataStore.GetAggregateEventDocuments(streamId, aggregateId, cancellationToken);
        if (aggregateEventDocumentsResult.IsNotSuccess)
        {
            return aggregateEventDocumentsResult.Failure!;
        }
        var aggregateEventDocuments = aggregateEventDocumentsResult.Value!;
        if (aggregateEventDocuments.Count == 0)
        {
            return new List<IDomainEvent>();
        }

        var eventDocumentsResult = await _cosmosDataStore.GetEventDocuments(streamId, aggregateEventDocuments.Select(ae => ae.EventId).ToArray(), cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        var eventDocuments = eventDocumentsResult.Value!;
        return eventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
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
            throw new InvalidOperationException($"Aggregate {typeof(TAggregate).Name} does not have a AggregateType attribute.");
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
            var eventTypes = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT VALUE MAX(c.sequence) FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType)";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", eventTypes);
        }

        try
        {
            using var iterator = _container.GetItemQueryIterator<int?>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(streamId.Id)
            });

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
            ex.AddException(streamId, operationDescription: "Get Latest Event Sequence");
            return ErrorHandling.DefaultFailure;
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
            DiagnosticsExtensions.AddActivityEvent(streamId, expectedEventSequence, latestEventSequence);
            return ErrorHandling.DefaultFailure;
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
            batchResponse.AddActivityEvent(streamId, aggregateId, aggregateType);
            return batchResponse.IsSuccessStatusCode
                ? Result.Ok()
                : new Failure
                (
                    Title: "Error",
                    Description: "There was an error when processing the request"
                );
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Save Aggregate");
            return ErrorHandling.DefaultFailure;
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
            DiagnosticsExtensions.AddActivityEvent(streamId, expectedEventSequence, latestEventSequence);
            return ErrorHandling.DefaultFailure;
        }

        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));
            var eventDocuments = new List<EventDocument>();
            foreach (var @event in domainEvents)
            {
                var eventDocument = @event.ToEventDocument(streamId, sequence: ++latestEventSequence);
                eventDocument.CreatedDate = timeStamp;
                eventDocument.CreatedBy = currentUserNameIdentifier;
                eventDocuments.Add(eventDocument);
                batch.CreateItem(eventDocument);
            }

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            batchResponse.AddActivityEvent(streamId, eventDocuments);
            return batchResponse.IsSuccessStatusCode
                ? Result.Ok()
                : new Failure
                (
                    Title: "Error",
                    Description: "There was an error when processing the request"
                );
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Save Domain Events");
            return ErrorHandling.DefaultFailure;
        }
    }

    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            throw new InvalidOperationException($"Aggregate {typeof(TAggregate).Name} does not have a AggregateType attribute.");
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

        return await _cosmosDataStore.UpdateAggregateDocument(streamId, aggregateId, aggregateDocument, cancellationToken);
    }

    public void Dispose() => _cosmosClient.Dispose();
}
