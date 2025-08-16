using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;

namespace OpenCqrs.Caching.Memory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddMemoryCache(this IOpenCqrsServiceBuilder builder)
        {
            return AddMemoryCache(builder, opt => { });
        }

        public static IOpenCqrsServiceBuilder AddMemoryCache(this IOpenCqrsServiceBuilder builder, Action<CacheOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Services.Configure(configureOptions);

            builder.Services.AddTransient<ICacheProvider, MemoryCacheProvider>();

            return builder;
        }
    }
}
