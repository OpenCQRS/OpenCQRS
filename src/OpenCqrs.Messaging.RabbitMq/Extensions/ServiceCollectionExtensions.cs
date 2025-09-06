using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenCqrs.Messaging.RabbitMq.Configuration;

namespace OpenCqrs.Messaging.RabbitMq.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ messaging provider to the service collection with custom options.
    /// </summary>
    public static IServiceCollection AddOpenCqrsRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> options)
    {
        services.Configure(options);
        services.AddSingleton<IMessagingProvider, RabbitMqMessagingProvider>();

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ messaging provider to the service collection with a connection string.
    /// </summary>
    public static IServiceCollection AddOpenCqrsRabbitMq(this IServiceCollection services, string connectionString)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ConnectionString = connectionString;
        });

        services.Replace(ServiceDescriptor.Scoped<IMessagingProvider>(test => new RabbitMqMessagingProvider(test.GetRequiredService<IOptions<RabbitMqOptions>>())));

        return services;
    }
}
