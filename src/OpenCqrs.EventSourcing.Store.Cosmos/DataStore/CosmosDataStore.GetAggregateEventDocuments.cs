using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

public partial class CosmosDataStore
{
    /// <summary>
    /// Retrieves all aggregate event documents for a specific aggregate from Cosmos DB.
    /// The results are ordered by applied date.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate whose events to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier containing the aggregate.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A result containing a list of aggregate event documents, or a failure if an error occurred.</returns>
    /// <exception cref="Exception">Thrown when the aggregate type does not have an AggregateType attribute.</exception>
    public async Task<Result<List<AggregateEventDocument>>> GetAggregateEventDocuments<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        const string sql = "SELECT * FROM c WHERE c.streamId = @streamId AND c.aggregateId = @aggregateId AND c.documentType = @documentType ORDER BY c.appliedDate";
        var queryDefinition = new QueryDefinition(sql)
            .WithParameter("@streamId", streamId.Id)
            .WithParameter("@aggregateId", aggregateId.ToStoreId())
            .WithParameter("@documentType", DocumentType.AggregateEvent);

        var aggregateEventDocuments = new List<AggregateEventDocument>();

        try
        {
            using var iterator = _container.GetItemQueryIterator<AggregateEventDocument>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(streamId.Id)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                aggregateEventDocuments.AddRange(response);
            }
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Get Aggregate Event Documents");
            return ErrorHandling.DefaultFailure;
        }

        return aggregateEventDocuments;
    }
}
