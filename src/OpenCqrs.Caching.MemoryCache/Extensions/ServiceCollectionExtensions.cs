using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Caching.MemoryCache.Configuration;

namespace OpenCqrs.Caching.MemoryCache.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsMemoryCache(this IServiceCollection services)
    {
        services.AddOpenCqrsMemoryCache(opt => { });
    }

    public static void AddOpenCqrsMemoryCache(this IServiceCollection services, Action<MemoryCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.Configure(options);

        services.AddSingleton<ICachingProvider, MemoryCacheProvider>();
    }
}
