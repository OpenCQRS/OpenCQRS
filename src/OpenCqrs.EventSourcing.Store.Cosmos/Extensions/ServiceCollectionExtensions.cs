using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.EventSourcing.DomainService;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

/// <summary>
/// Provides extension methods for configuring Cosmos DB Event Sourcing store services in the dependency injection container.
/// These methods simplify the registration of all necessary services and dependencies for the Cosmos DB implementation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Cosmos DB Event Sourcing store services and their dependencies in the service collection.
    /// This method configures all necessary services, including the domain service, data store, and setup components.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="options">An action to configure the Cosmos DB connection and database options.</param>
    public static void AddOpenCqrsCosmos(this IServiceCollection services, Action<CosmosOptions> options)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddOptions<CosmosOptions>().Configure(options);
        services.TryAddScoped<IDomainService, CosmosDomainService>();
        services.TryAddScoped<ICosmosDataStore, CosmosDataStore>();
        services.TryAddSingleton<CosmosSetup>();
    }
}
