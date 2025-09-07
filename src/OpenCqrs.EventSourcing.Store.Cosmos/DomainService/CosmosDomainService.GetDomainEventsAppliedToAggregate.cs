using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

public partial class CosmosDomainService
{
    /// <summary>
    /// Gets domain events that have been applied to a specific aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of domain events applied to the aggregate or failure information.</returns>
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
}
