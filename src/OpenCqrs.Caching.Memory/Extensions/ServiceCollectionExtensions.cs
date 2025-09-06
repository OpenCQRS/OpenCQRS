using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace OpenCqrs.Caching.Memory.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsMemoryCache(this IServiceCollection services)
    {
        services.AddOpenCqrsMemoryCache(opt => { });
    }

    public static void AddOpenCqrsMemoryCache(this IServiceCollection services, Action<Configuration.MemoryCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.Configure(options);

        services.AddMemoryCache();

        var serviceProvider = services.BuildServiceProvider();
        var memoryCacheOptions = serviceProvider.GetService<IOptions<Configuration.MemoryCacheOptions>>();

        services.Replace(ServiceDescriptor.Scoped<ICachingProvider>(sp => new MemoryCacheProvider(sp.GetRequiredService<IMemoryCache>(), memoryCacheOptions!)));
    }
}
