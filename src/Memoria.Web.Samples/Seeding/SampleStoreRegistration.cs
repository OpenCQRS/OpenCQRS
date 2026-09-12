using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Extensions;
using Memoria.Web.Data;
using Memoria.Web.Samples.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Wires up whichever store the seeder was pointed at.
/// </summary>
/// <remarks>
/// The same two shapes the web tool meets, wired the same way and in the same order, because the
/// seeder reads the same connection string and must reach the same store. A relational store is four
/// registrations and a context each; a Cosmos store is one client and the SDK-based domain service
/// over it, with no context at all — its model carries index definitions the Cosmos provider refuses,
/// and there is no dynamic consistency boundary store for it.
/// <para>
/// Called after <c>AddMemoriaEventSourcing</c>, whose no-op <c>IDomainService</c> the store's own
/// must come after to replace.
/// </para>
/// </remarks>
public static class SampleStoreRegistration
{
    /// <summary>
    /// Registers the store the sample data is written to, and the store itself as the run sees it.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="database">The store the seeder was pointed at.</param>
    /// <param name="configuration">Where the Cosmos database and container names are read from.</param>
    public static IServiceCollection AddSampleStore(
        this IServiceCollection services, DatabaseConnection database, IConfiguration configuration)
    {
        if (database.Provider is DatabaseProvider.Cosmos)
        {
            var store = CosmosStore.Of(database, configuration);

            // The same wiring the tool uses, shared rather than written again, so both reach the same
            // container from the same connection string and the same two settings.
            services.AddSingleton(store);
            services.AddCosmosStreamedStore(store);

            services.AddScoped<ISampleStore, CosmosSampleStore>();

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

        services.AddScoped<ISampleStore, RelationalSampleStore>();

        return services;
    }
}
