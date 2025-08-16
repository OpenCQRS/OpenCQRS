using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;
using OpenCqrs.Store.EF.Extensions;

namespace OpenCqrs.Store.EF.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddInMemoryStore(this IOpenCqrsServiceBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddEFStore();

            builder.Services.AddDbContext<DomainDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: "DomainDb"));

            builder.Services.AddTransient<IDatabaseProvider, InMemoryDatabaseProvider>();

            return builder;
        }
    }
}
