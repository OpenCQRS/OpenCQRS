using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenCqrs.Caching.Redis.Configuration;
using StackExchange.Redis;

namespace OpenCqrs.Caching.Redis.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsRedisCache(this IServiceCollection services)
    {
        services.AddOpenCqrsRedisCache(_ => { });
    }

    public static void AddOpenCqrsRedisCache(this IServiceCollection services, Action<RedisCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.Configure(options);

        var serviceProvider = services.BuildServiceProvider();
        var redisCacheOptions = serviceProvider.GetService<IOptions<RedisCacheOptions>>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCacheOptions!.Value.ConnectionString));
        services.Replace(ServiceDescriptor.Scoped<ICachingProvider>(sp => new RedisCacheProvider(sp.GetRequiredService<IConnectionMultiplexer>(), redisCacheOptions!)));
    }
}
