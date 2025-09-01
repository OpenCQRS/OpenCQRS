using Microsoft.Azure.Cosmos;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public class CosmosSetup(CosmosClient client)
{
    public async Task<Container> CreateContainerWithIndexingPolicyAsync(string dbName, string containerName)
    {
        var database = await client.CreateDatabaseIfNotExistsAsync(dbName);

        var containerProperties = new ContainerProperties(containerName, partitionKeyPath: "/aggregateId")
        {
            IndexingPolicy = new IndexingPolicy
            {
                // Automatic indexing enabled
                Automatic = true,

                // Consistent = all writes are immediately indexed
                IndexingMode = IndexingMode.Consistent,

                // Exclude everything by default
                ExcludedPaths =
                {
                    new ExcludedPath { Path = "/*" }
                },

                // Include only specific paths
                IncludedPaths =
                {
                    new IncludedPath { Path = "/aggregateId/?" },
                    new IncludedPath { Path = "/type/?" },
                    new IncludedPath { Path = "/eventType/?" },
                    new IncludedPath { Path = "/sequence/?" }
                }
            }
        };

        var response = await database.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
        return response.Container;
    }
}
