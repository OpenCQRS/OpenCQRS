using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenCqrs.Extensions;
using OpenCqrs.Store.EF.Cosmos.Configuration;
using OpenCqrs.Store.EF.Extensions;

namespace OpenCqrs.Store.EF.Cosmos.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddCosmosStore(this IOpenCqrsServiceBuilder builder)
        {
            return AddCosmosStore(builder, opt => { });
        }

        public static IOpenCqrsServiceBuilder AddCosmosStore(this IOpenCqrsServiceBuilder builder, Action<CosmosDbOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Services.Configure(configureOptions);

            var serviceProvider = builder.Services.BuildServiceProvider();
            var dbOptions = serviceProvider.GetService<IOptions<CosmosDbOptions>>().Value;

            builder.AddEFStore();

            builder.Services.AddDbContext<DomainDbContext>(options =>
                options.UseCosmos(dbOptions.ConnectionString, dbOptions.DatabaseName));

            builder.Services.AddTransient<IDatabaseProvider, CosmosDatabaseProvider>();

            return builder;
        }
    }
}
