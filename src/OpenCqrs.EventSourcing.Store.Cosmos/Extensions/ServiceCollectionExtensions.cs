using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsCosmos(this IServiceCollection services, Action<CosmosOptions> configureOptions)
    {
        services.AddOptions<CosmosOptions>().Configure(configureOptions);
        services.TryAddScoped<IDomainService, CosmosDomainService>();
        services.TryAddScoped<ICosmosDataStore, CosmosDataStore>();
        
        // TODO: Container throughput (shared or dedicated)

        // TODO: ContainerProperties with partition key, indexing policy, etc.
    }
}
