using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosSetup(IOptions<CosmosOptions> cosmosOptions)
{
    public async Task<Container> CreateDatabaseAndContainerIfNotExist(int throughput = 400)
    {
        var cosmosClient = new CosmosClient(cosmosOptions.Value.Endpoint, cosmosOptions.Value.AuthKey, cosmosOptions.Value.ClientOptions);
        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosOptions.Value.DatabaseName);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(cosmosOptions.Value.ContainerName, "/streamId", throughput);
        return containerResponse.Container;
    }
}