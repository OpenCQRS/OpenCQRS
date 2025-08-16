using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;
using OpenCqrs.UI.Configuration;
using OpenCqrs.UI.Queries;

namespace OpenCqrs.UI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the service bus provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IOpenCqrsServiceBuilder AddUI(this IOpenCqrsServiceBuilder builder)
        {
            return AddUI(builder, opt => {});
        }

        /// <summary>
        /// Adds the service bus provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="setupAction"></param>
        /// <returns></returns>
        public static IOpenCqrsServiceBuilder AddUI(this IOpenCqrsServiceBuilder builder, Action<UIOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure(setupAction);

            builder.Services.Scan(s => s
                .FromAssembliesOf(typeof(GetAggregateModel))
                .AddClasses()
                .AsImplementedInterfaces());

            return builder;
        }
    }
}
