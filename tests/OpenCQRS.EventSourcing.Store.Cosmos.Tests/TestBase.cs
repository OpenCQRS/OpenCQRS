using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
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
    protected readonly ICosmosDataStore DataStore;
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
        
        var cosmosOptions = new CosmosOptions
        {
            Endpoint = "https://localhost:8081",
            AuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        };

        var optionsSubstitute = Substitute.For<IOptions<CosmosOptions>>();
        optionsSubstitute.Value.Returns(cosmosOptions);
        
        TimeProvider = new FakeTimeProvider();
        var httpContextAccessor = CreateHttpContextAccessor();
        DataStore = new CosmosDataStore(optionsSubstitute, TimeProvider, httpContextAccessor);
        DomainService = new CosmosDomainService(optionsSubstitute, TimeProvider, httpContextAccessor, DataStore);

        var cosmosClient = new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.AuthKey, cosmosOptions.ClientOptions);
        var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosOptions.DatabaseName).GetAwaiter().GetResult();
        databaseResponse.Database.CreateContainerIfNotExistsAsync(cosmosOptions.ContainerName, "/streamId", throughput: 400);
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
