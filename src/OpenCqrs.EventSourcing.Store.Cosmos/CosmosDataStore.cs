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

public class CosmosDataStore : ICosmosDataStore
{
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public CosmosDataStore(ICosmosClientConnection cosmosClientConnection, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _cosmosClient = new CosmosClient(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
        _container = _cosmosClient.GetContainer(cosmosClientConnection.DatabaseName, cosmosClientConnection.ContainerName);
    }

    public async Task<Result<AggregateDocument?>> GetAggregateDocument<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
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

        var aggregateDocumentId = aggregateId.ToIdWithTypeVersion(aggregateType.Version);

        try
        {
            var response = await _container.ReadItemAsync<AggregateDocument>(aggregateDocumentId, new PartitionKey(streamId.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (AggregateDocument?)null;
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the aggregate document", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the aggregate document",
                Description: "There was an error when retrieving the aggregate document"
            );
        }
    }

    public async Task<Result<List<EventDocument>>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;

        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }

        var eventDocuments = new List<EventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                eventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the event documents", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the event documents",
                Description: "There was an error when retrieving the event documents"
            );
        }

        return eventDocuments;
    }

    public async Task<Result<List<EventDocument>>> GetEventDocumentsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;

        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence >= @fromSequence AND c.documentType = @documentType ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@fromSequence", fromSequence)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence >= @fromSequence AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@fromSequence", fromSequence)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }

        var eventDocuments = new List<EventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                eventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the event documents", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the event documents",
                Description: "There was an error when retrieving the event documents"
            );
        }

        return eventDocuments;
    }

    public async Task<Result<List<EventDocument>>> GetEventDocumentsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;

        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence <= @upToSequence AND c.documentType = @documentType ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@upToSequence", upToSequence)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var domainEventTypeKeys = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence <= @upToSequence AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@upToSequence", upToSequence)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", domainEventTypeKeys);
        }

        var eventDocuments = new List<EventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                eventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when retrieving the event documents", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error retrieving the event documents",
                Description: "There was an error when retrieving the event documents"
            );
        }

        return eventDocuments;
    }

    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, AggregateDocument aggregateDocument, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregate = aggregateDocument.ToAggregate<TAggregate>();

        var aggregateTypeAttribute = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateTypeAttribute == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }

        var currentAggregateVersion = aggregate.Version;

        var newEventDocumentsResult = await GetEventDocumentsFromSequence(streamId, fromSequence: aggregate.LatestEventSequence + 1, aggregate.EventTypeFilter, cancellationToken);
        if (newEventDocumentsResult.IsNotSuccess)
        {
            return newEventDocumentsResult.Failure!;
        }
        var newEventDocuments = newEventDocumentsResult.Value!;
        if (newEventDocuments.Count == 0)
        {
            return aggregate;
        }

        var newDomainEvents = newEventDocuments.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
        aggregate.Apply(newDomainEvents);

        if (aggregate.Version == currentAggregateVersion)
        {
            return aggregate;
        }

        var newLatestEventSequenceForAggregate = newEventDocuments.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        var timeStamp = _timeProvider.GetUtcNow();
        var currentUserNameIdentifier = _httpContextAccessor.GetCurrentUserNameIdentifier();

        try
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId.Id));

            foreach (var eventDocument in newEventDocuments)
            {
                var aggregateEventDocument = new AggregateEventDocument
                {
                    Id = $"{aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version)}:{eventDocument.Id}",
                    StreamId = streamId.Id,
                    AggregateId = aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version),
                    EventId = eventDocument.Id,
                    AppliedDate = timeStamp
                };
                batch.CreateItem(aggregateEventDocument);
            }

            var aggregateDocumentToUpdate = aggregate.ToAggregateDocument(streamId, aggregateId, newLatestEventSequenceForAggregate);
            aggregateDocumentToUpdate.CreatedDate = aggregateDocument.CreatedDate;
            aggregateDocumentToUpdate.CreatedBy = aggregateDocument.CreatedBy;
            aggregateDocumentToUpdate.UpdatedDate = timeStamp;
            aggregateDocumentToUpdate.UpdatedBy = currentUserNameIdentifier;

            var batchResponse = await batch.ExecuteAsync(cancellationToken);
            if (batchResponse.IsSuccessStatusCode)
            {
                return aggregate;
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
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when updating the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving changes",
                Description: "There was an error when updating the aggregate"
            );
        }
    }
}
