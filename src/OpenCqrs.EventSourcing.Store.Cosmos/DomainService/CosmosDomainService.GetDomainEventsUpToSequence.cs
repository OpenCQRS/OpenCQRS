using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets domain events up to a specific sequence number with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="upToSequence">The sequence number to stop at.</param>
    /// <param name="eventTypeFilter">Optional filter for specific event types.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of domain events or failure information.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocumentsUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }
}
