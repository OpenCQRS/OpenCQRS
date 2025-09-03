using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.Messaging;
using OpenCqrs.Messaging.ServiceBus.Configuration;

namespace OpenCqrs.Messaging.ServiceBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsServiceBus(this IServiceCollection services, Action<ServiceBusOptions> options)
    {
        services.AddOptions<ServiceBusOptions>().Configure(options);
        services.TryAddScoped<IMessagingProvider, ServiceBusMessagingProvider>();
    }
}
