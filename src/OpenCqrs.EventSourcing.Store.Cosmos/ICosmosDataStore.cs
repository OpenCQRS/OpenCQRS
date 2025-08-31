using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public interface ICosmosDataStore
{
    Task<List<EventDocument>> GetEventDocuments(IStreamId streamId, Type[]? eventTypeFilter, CancellationToken cancellationToken = default);
}