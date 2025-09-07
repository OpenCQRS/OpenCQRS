using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsBetweenSequences(
        IStreamId streamId,
        int fromSequence,
        int toSequence,
        Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        var eventDocumentsResult = await _cosmosDataStore.GetEventDocumentsBetweenSequences(streamId, fromSequence, toSequence, eventTypeFilter, cancellationToken);
        if (eventDocumentsResult.IsNotSuccess)
        {
            return eventDocumentsResult.Failure!;
        }
        return eventDocumentsResult.Value!.Select(eventDocument => eventDocument.ToDomainEvent()).ToList();
    }
}
