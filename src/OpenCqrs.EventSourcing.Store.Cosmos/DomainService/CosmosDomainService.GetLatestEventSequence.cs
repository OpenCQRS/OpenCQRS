using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets the latest event sequence number for a stream with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventTypeFilter">Optional filter for specific event types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the latest event sequence number or failure information.</returns>
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
}
