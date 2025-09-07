using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

public partial class CosmosDataStore
{
    /// <summary>
    /// Retrieves event documents from a stream up to a specific sequence number, optionally filtered by event types.
    /// The results are ordered by sequence number.
    /// </summary>
    /// <param name="streamId">The stream identifier to retrieve events from.</param>
    /// <param name="upToSequence">The maximum sequence number to retrieve events up to (inclusive).</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter by. If null or empty, all events are returned.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A result containing a list of event documents up to the specified sequence, or a failure if an error occurred.</returns>
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
            var eventTypes = eventTypeFilter!
                .Select(eventType => TypeBindings.DomainEventTypeBindings.FirstOrDefault(b => b.Value == eventType))
                .Select(b => b.Key).ToList();

            const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.sequence <= @upToSequence AND c.documentType = @documentType AND ARRAY_CONTAINS(@eventTypes, c.eventType) ORDER BY c.sequence";
            queryDefinition = new QueryDefinition(sql)
                .WithParameter("@streamId", streamId.Id)
                .WithParameter("@upToSequence", upToSequence)
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
            ex.AddException(streamId, operationDescription: "Get Event Documents up to Sequence");
            return ErrorHandling.DefaultFailure;
        }

        return eventDocuments;
    }
}
