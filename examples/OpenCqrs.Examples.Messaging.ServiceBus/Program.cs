// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs;
using OpenCqrs.Examples.Messaging.ServiceBus.Commands;
using OpenCqrs.Extensions;
using OpenCqrs.Messaging.ServiceBus.Configuration;
using OpenCqrs.Messaging.ServiceBus.Extensions;

var serviceProvider = ConfigureServices();

var dispatcher = serviceProvider.GetService<IDispatcher>();

var customerId = Guid.NewGuid();
var orderId = Guid.NewGuid();

var sendAndPublishResponse = await dispatcher!.SendAndPublish(new PlaceOrderCommand(customerId, orderId, Amount: 25m));
Console.WriteLine($"Order placed. Service bus messages sent: {sendAndPublishResponse.MessageResults}");

Console.ReadLine();
return;

IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    services.AddSingleton(TimeProvider.System);
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    
    const string connectionString = "your-service-bus-connection-string";

    services.AddOpenCqrs(typeof(Program));
    services.AddOpenCqrsServiceBus(new ServiceBusOptions{ConnectionString = connectionString});

    return services.BuildServiceProvider();
}
