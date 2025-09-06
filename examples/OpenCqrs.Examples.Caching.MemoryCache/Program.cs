// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using OpenCqrs;
using OpenCqrs.Caching.MemoryCache.Extensions;
using OpenCqrs.Examples.Caching.MemoryCache.Queries;
using OpenCqrs.Extensions;

var serviceProvider = ConfigureServices();

var dispatcher = serviceProvider.GetService<IDispatcher>();

var result = await dispatcher!.Get(new GetSomethingQuery
{
    CacheKey = "my-cache-key",
    CacheTimeInSeconds = 600
});
Console.WriteLine($"Something retrieved. Result: {result.Value}.");

Console.ReadLine();
return;

IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    services.AddOpenCqrs(typeof(Program));
    services.AddOpenCqrsMemoryCache();

    return services.BuildServiceProvider();
}
