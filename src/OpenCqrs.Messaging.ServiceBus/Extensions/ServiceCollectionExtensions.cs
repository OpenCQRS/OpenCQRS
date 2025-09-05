using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.Messaging.ServiceBus.Configuration;

namespace OpenCqrs.Messaging.ServiceBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsServiceBus(this IServiceCollection services, ServiceBusOptions options)
    {
        var serviceBusClient = new ServiceBusClient(options.ConnectionString);
        services.Replace(ServiceDescriptor.Scoped<IMessagingProvider>(_ => new ServiceBusMessagingProvider(serviceBusClient)));
    }
}
