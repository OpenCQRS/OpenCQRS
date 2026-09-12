using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
using Memoria.EventSourcing.Store.Cosmos;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Extensions;
using Memoria.Web.Extensibility;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Data;

/// <summary>
/// Wires up whichever store the tool was pointed at.
/// </summary>
/// <remarks>
/// Two shapes rather than one with a provider in it. A relational store is read through contexts
/// Entity Framework Core opens; a Cosmos store is read through the SDK the store itself writes with,
/// and has no context to open — its model carries index definitions the Cosmos provider refuses, and
/// there is no dynamic consistency boundary store for it at all. Registering the relational half
/// anyway would leave four contexts nothing can resolve. The Cosmos branch does register the
/// streamed write path (the SDK-based <see cref="IDomainService"/>) so a snapshot can be refreshed,
/// but it wires that onto the same <see cref="CosmosClient"/> the reads use rather than opening a
/// second one; there is still no dynamic consistency boundary service for it.
/// </remarks>
public static class StoreRegistration
{
    /// <summary>
    /// Registers the store, and the reader the streamed pages ask their two questions of.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="database">The store the tool was pointed at.</param>
    /// <param name="configuration">Where the Cosmos database and container names are read from.</param>
    public static IServiceCollection AddStore(
        this IServiceCollection services, DatabaseConnection database, IConfiguration configuration)
    {
        // What the pages may offer, decided once here rather than by each of them asking which
        // engine it is.
        services.AddSingleton(StoreCapabilities.Of(database.Provider));

        if (database.Provider is DatabaseProvider.Cosmos)
        {
            var store = CosmosStore.Of(database, configuration);

            // The client, and the streamed store's own read and write path over it — the write half
            // being what lets the Update tab refresh a snapshot. Shared with the sample seeder, so
            // both reach the same container from the same connection string.
            services.AddCosmosStreamedStore(store);

            // The tool's own questions, which are asked of the documents directly rather than
            // through the framework: they are the two the pages ask, and no store operation answers
            // them. Read through the one client registered above.
            services.AddScoped<IStreamedReads>(provider => new CosmosStreamedReads(
                provider.GetRequiredService<CosmosClient>(), store.DatabaseName, store.ContainerName));

            return services;
        }

        // Both contexts take their options as the base type's DbContextOptions rather than their own
        // closed type, so each is registered against that.
        services.AddScoped(serviceProvider =>
        {
            var options = new DbContextOptionsBuilder<DomainDbContext>();
            database.Apply(options).UseApplicationServiceProvider(serviceProvider);
            return options.Options;
        });

        services.AddScoped(serviceProvider =>
        {
            var options = new DbContextOptionsBuilder<DcbDbContext>();
            database.Apply(options).UseApplicationServiceProvider(serviceProvider);
            return options.Options;
        });

        services.AddDbContext<StreamedStoreDbContext>(options => database.Apply(options));
        services.AddDbContext<DcbStoreDbContext>(options => database.Apply(options));

        services.AddMemoriaEntityFrameworkCore<StreamedStoreDbContext>();
        services.AddMemoriaDcbEntityFrameworkCore<DcbStoreDbContext>();

        services.AddScoped<IStreamedReads, EfStreamedReads>();

        return services;
    }
}
