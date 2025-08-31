using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using OpenCqrs.EventSourcing;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Events;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests;

public abstract class TestBase
{
    protected readonly IDomainService DomainService;
    protected readonly ICosmosDataStore CosmosDataStore;
    protected readonly FakeTimeProvider TimeProvider;

    protected TestBase()
    {
        TypeBindings.DomainEventTypeBindings = new Dictionary<string, Type>
        {
            {"TestAggregateCreated:1", typeof(TestAggregateCreatedEvent)},
            {"TestAggregateUpdated:1", typeof(TestAggregateUpdatedEvent)},
            {"SomethingHappened:1", typeof(SomethingHappenedEvent)}
        };

        TypeBindings.AggregateTypeBindings = new Dictionary<string, Type>
        {
            {"TestAggregate1:1", typeof(TestAggregate1)},
            {"TestAggregate2:1", typeof(TestAggregate2)}
        };

        const string endpoint = "https://localhost:8081";
        const string authKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        const string databaseName = "OpenCQRS";
        const string containerName = "Domain";
        var cosmosClientConnection = new CosmosClientConnection(endpoint, authKey, databaseName, containerName, new CosmosClientOptions { ApplicationName = "OpenCQRS", ConnectionMode = ConnectionMode.Direct });
        TimeProvider = new FakeTimeProvider();
        CosmosDataStore = new CosmosDataStore(cosmosClientConnection, TimeProvider);
        DomainService = new CosmosDomainService(cosmosClientConnection, TimeProvider, CreateHttpContextAccessor(), CosmosDataStore);

        var cosmosClient = new CosmosClient(cosmosClientConnection.Endpoint, cosmosClientConnection.AuthKey, cosmosClientConnection.ClientOptions);
        var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).GetAwaiter().GetResult();
        databaseResponse.Database.CreateContainerIfNotExistsAsync(containerName, "/streamId", throughput: 400);
    }
    
    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var context = new DefaultHttpContext();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "TestUser")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        context.User = principal;

        httpContextAccessor.HttpContext.Returns(context);
        return httpContextAccessor;
    }
}
