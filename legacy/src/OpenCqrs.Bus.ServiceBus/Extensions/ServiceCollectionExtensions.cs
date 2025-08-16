using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Bus.ServiceBus.Factories;
using OpenCqrs.Extensions;

namespace OpenCqrs.Bus.ServiceBus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddServiceBus(this IOpenCqrsServiceBuilder builder)
        {
            return AddServiceBus(builder, opt => { });
        }

        /// <summary>
        /// Adds the service bus provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IOpenCqrsServiceBuilder AddServiceBus(this IOpenCqrsServiceBuilder builder, Action<BusOptions> configureOptions)
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

            builder.Services
                .AddTransient<IBusMessageDispatcher, BusMessageDispatcher>()
                .AddTransient<IBusProvider, ServiceBusProvider>()
                .AddTransient<IMessageFactory, MessageFactory>();

            return builder;
        }
    }
}
