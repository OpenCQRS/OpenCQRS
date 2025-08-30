using Microsoft.Azure.Cosmos;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosDataStore(ICosmosClientConnection cosmosClientConnection)
{
    private readonly CosmosClient _cosmosClient = new(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
}
