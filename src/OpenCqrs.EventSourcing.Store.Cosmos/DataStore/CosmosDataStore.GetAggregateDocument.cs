using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;
using OpenCqrs.EventSourcing.Store.Cosmos.Extensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

public partial class CosmosDataStore
{
    /// <summary>
    /// Retrieves an aggregate document from Cosmos DB for the specified stream and aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier containing the aggregate.</param>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A result containing the aggregate document if found, null if not found, or a failure if an error occurred.</returns>
    /// <exception cref="Exception">Thrown when the aggregate type does not have an AggregateType attribute.</exception>
    public async Task<Result<AggregateDocument?>> GetAggregateDocument<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateDocumentId = aggregateId.ToStoreId();

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
            ex.AddException(streamId, operationDescription: "Get Aggregate Document");
            return ErrorHandling.DefaultFailure;
        }
    }
}
