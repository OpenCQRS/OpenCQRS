using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Caching.Redis.Configuration;

namespace OpenCqrs.Caching.Redis.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsRedisCache(this IServiceCollection services)
    {
        services.AddOpenCqrsRedisCache(opt => { });
    }

    public static void AddOpenCqrsRedisCache(this IServiceCollection services, Action<RedisCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.Configure(options);

        services.AddSingleton<ICachingProvider, RedisProvider>();
    }
}
