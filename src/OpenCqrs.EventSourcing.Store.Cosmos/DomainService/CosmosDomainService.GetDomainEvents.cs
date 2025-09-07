using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets all domain events from a stream with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventTypeFilter">Optional filter for specific event types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of domain events or failure information.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocuments(streamId, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }
}
