using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Bus.RabbitMQ.Factories;
using OpenCqrs.Extensions;

// ReSharper disable InconsistentNaming

namespace OpenCqrs.Bus.RabbitMQ.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddRabbitMQ(this IOpenCqrsServiceBuilder builder)
        {
            return AddRabbitMQ(builder, opt => { });
        }

        /// <summary>
        /// Adds the service bus provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IOpenCqrsServiceBuilder AddRabbitMQ(this IOpenCqrsServiceBuilder builder, Action<BusOptions> configureOptions)
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
                .AddTransient<IBusProvider, RabbitMQBusProvider>()
                .AddTransient<IMessageFactory, MessageFactory>();

            return builder;
        }
    }
}
