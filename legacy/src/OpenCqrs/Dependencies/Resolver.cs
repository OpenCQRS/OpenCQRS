using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace OpenCqrs.Dependencies
{
    public class Resolver : IResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public Resolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Resolve<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return _serviceProvider.GetServices<T>();
        }

        public object Resolve(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            return _serviceProvider.GetServices(type);
        }
    }
}
