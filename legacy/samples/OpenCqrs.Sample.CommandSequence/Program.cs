using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;
using OpenCqrs.Sample.CommandSequence.Commands;
using OpenCqrs.Utilities;
using OpenCqrs.Validation.FluentValidation.Extensions;

namespace OpenCqrs.Sample.CommandSequence
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();

            var dispatcher = serviceProvider.GetService<IDispatcher>();

            var result = AsyncUtil.RunSync(() => dispatcher.SendAsync<string>(new SampleCommandSequence()));

            Console.WriteLine($"Final result: {result}");

            Console.ReadLine();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services
                .AddOpenCqrs(typeof(Program))
                .AddFluentValidation();

            return services.BuildServiceProvider();
        }
    }
}
