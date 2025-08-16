using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Domain;
using OpenCqrs.Extensions;
using OpenCqrs.Store.Cosmos.Sql.Configuration;
using OpenCqrs.Store.Cosmos.Sql.Documents;
using OpenCqrs.Store.Cosmos.Sql.Documents.Factories;
using OpenCqrs.Store.Cosmos.Sql.Repositories;

namespace OpenCqrs.Store.Cosmos.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddCosmosSqlStore(this IOpenCqrsServiceBuilder builder, IConfiguration configuration)
        {
            return AddCosmosSqlStore(builder, configuration, opt => { });
        }

        public static IOpenCqrsServiceBuilder AddCosmosSqlStore(this IOpenCqrsServiceBuilder builder, IConfiguration configuration, Action<CosmosDbOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure<OpenCqrsCosmosSqlConfiguration>(configuration.GetSection("OpenCqrsCosmosSqlConfiguration"));
            builder.Services.Configure(setupAction);

            var endpoint = configuration.GetSection("OpenCqrsCosmosSqlConfiguration:ServerEndpoint").Value;
            var key = configuration.GetSection("OpenCqrsCosmosSqlConfiguration:AuthKey").Value;
            builder.Services.AddSingleton<IDocumentClient>(x => new DocumentClient(new Uri(endpoint), key));

            builder.Services
                .AddTransient<IStoreProvider, CosmosStoreProvider>();

            builder.Services
                .AddTransient<IAggregateDocumentFactory, AggregateDocumentFactory>()
                .AddTransient<ICommandDocumentFactory, CommandDocumentFactory>()
                .AddTransient<IEventDocumentFactory, EventDocumentFactory>();

            builder.Services
                .AddTransient<IDocumentRepository<AggregateDocument>, AggregateRepository>()
                .AddTransient<IDocumentRepository<CommandDocument>, CommandRepository>()
                .AddTransient<IDocumentRepository<EventDocument>, EventRepository>();

            return builder;
        }
    }
}
