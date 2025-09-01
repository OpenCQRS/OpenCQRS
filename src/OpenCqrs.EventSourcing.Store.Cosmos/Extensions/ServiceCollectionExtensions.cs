using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsCosmos(this IServiceCollection services, Action<CosmosOptions> options)
    {
        services.AddOptions<CosmosOptions>().Configure(options);
        services.TryAddScoped<IDomainService, CosmosDomainService>();
        services.TryAddScoped<ICosmosDataStore, CosmosDataStore>();
        services.TryAddSingleton<CosmosSetup>();
    }
}
