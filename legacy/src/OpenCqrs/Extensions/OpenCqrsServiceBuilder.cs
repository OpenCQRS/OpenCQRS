using System;
using Microsoft.Extensions.DependencyInjection;

namespace OpenCqrs.Extensions
{
    public class OpenCqrsServiceBuilder : IOpenCqrsServiceBuilder
    {
        public IServiceCollection Services { get; }

        public OpenCqrsServiceBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
}