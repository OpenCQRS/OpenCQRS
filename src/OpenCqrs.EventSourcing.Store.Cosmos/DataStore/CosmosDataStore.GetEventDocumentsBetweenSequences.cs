using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

public partial class CosmosDataStore
{
    public async Task<Result<List<EventDocument>>> GetEventDocumentsBetweenSequences(IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter, CancellationToken cancellationToken = default)
    {
        QueryDefinition queryDefinition;

        var filterEventTypes = eventTypeFilter is not null && eventTypeFilter.Length > 0;
        if (!filterEventTypes)
        {
            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence >= @fromSequence AND c.sequence <= @toSequence AND c.documentType = @documentType ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@fromSequence", fromSequence)
                .WithParameter("@toSequence", toSequence)
                .WithParameter("@documentType", DocumentType.Event);
        }
        else
        {
            var eventTypes = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence >= @fromSequence AND c.sequence <= @toSequence AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@fromSequence", fromSequence)
                .WithParameter("@toSequence", toSequence)
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@eventTypes", eventTypes);
        }

        var eventDocuments = new List<EventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<EventDocument>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(streamId.Id)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                eventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Get Event Documents from Sequence");
            return ErrorHandling.DefaultFailure;
        }

        return eventDocuments;
    }
}
