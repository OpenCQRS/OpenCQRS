using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsCosmos(this IServiceCollection services, string endpoint, string authKey, string databaseName, string containerName, CosmosClientOptions? clientOptions = null)
    {
        // TODO: Add options pattern
        
        services.TryAddSingleton<ICosmosClientConnection>(new CosmosClientConnection(endpoint, authKey, databaseName, containerName, clientOptions));
        
        // TODO: Container throughput (shared or dedicated)
    }
}
