using System;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;

// ReSharper disable InconsistentNaming

namespace OpenCqrs.Store.EF.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddEFStore(this IOpenCqrsServiceBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Scan(s => s
                .FromAssembliesOf(typeof(DomainDbContext))
                .AddClasses()
                .AsImplementedInterfaces());

            return builder;
        }
    }
}
