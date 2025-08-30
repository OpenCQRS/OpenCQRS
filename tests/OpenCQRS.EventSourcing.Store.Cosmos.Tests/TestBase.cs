using OpenCqrs.EventSourcing.Store.Cosmos;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests;

public abstract class TestBase
{
    protected readonly ICosmosDataStore CosmosDataStore;

    protected TestBase()
    {
        var cosmosClientConnection = new CosmosClientConnection(Endpoint: "https://localhost:8081", AuthKey: "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", DatabaseName: "OpenCQRS");
        CosmosDataStore = new CosmosDataStore(cosmosClientConnection);
    }
}
