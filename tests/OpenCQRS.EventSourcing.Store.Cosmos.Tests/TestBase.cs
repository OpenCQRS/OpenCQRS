using OpenCqrs.EventSourcing;
using OpenCqrs.EventSourcing.Store.Cosmos;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests;

public abstract class TestBase
{
    protected readonly IDomainService DomainService;

    protected TestBase()
    {
        const string endpoint = "https://localhost:8081";
        const string authKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        const string databaseName = "OpenCQRS";
        const string containerName = "Domain";
        var cosmosClientConnection = new CosmosClientConnection(endpoint, authKey, databaseName, containerName);
        DomainService = new CosmosDomainService(cosmosClientConnection);
    }
}
