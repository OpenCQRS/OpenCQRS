using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.Cosmos.Tests.Models.Events;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly IDomainService DomainService;
    protected readonly ICosmosDataStore DataStore;
    protected readonly FakeTimeProvider TimeProvider;

    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

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

        var optionsSubstitute = Substitute.For<IOptions<CosmosOptions>>();
        optionsSubstitute.Value.Returns(new CosmosOptions
        {
            Endpoint = "https://localhost:8081",
            AuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        });
        TimeProvider = new FakeTimeProvider();
        var httpContextAccessor = CreateHttpContextAccessor();

        DataStore = new CosmosDataStore(optionsSubstitute, TimeProvider, httpContextAccessor);
        DomainService = new CosmosDomainService(optionsSubstitute, TimeProvider, httpContextAccessor, DataStore);

        var cosmosSetup = new CosmosSetup(optionsSubstitute);
        _ = cosmosSetup.CreateDatabaseAndContainerIfNotExist();

        _activitySource = new ActivitySource("TestSource");

        _activityListener = new ActivityListener();
        _activityListener.ShouldListenTo = _ => true;
        _activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
        _activityListener.SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData;
        _activityListener.ActivityStarted = _ => { };
        _activityListener.ActivityStopped = _ => { };

        ActivitySource.AddActivityListener(_activityListener);

        _activitySource.StartActivity();
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

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        GC.SuppressFinalize(this);
    }
}
