using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
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
/// anyway would leave four contexts nothing can resolve.
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

            services.AddSingleton(_ => new CosmosClient(store.ConnectionString, new CosmosClientOptions
            {
                // The pages ask across streams, so every read is a cross-partition query. Gateway
                // mode is the one that works wherever the tool is run, including from behind the
                // proxies an operator's machine tends to sit behind.
                ConnectionMode = ConnectionMode.Gateway
            }));

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

/// <summary>
/// Where a Cosmos store's documents are, which its connection string does not say.
/// </summary>
/// <param name="ConnectionString">The account, as given.</param>
/// <param name="DatabaseName">The database the container is in.</param>
/// <param name="ContainerName">The container the store writes into.</param>
/// <remarks>
/// A Cosmos connection string names an account and nothing more, so the database and the container
/// are asked for separately. They default to what <c>CosmosOptions</c> defaults to, so a store
/// installed under those names needs no settings at all.
/// </remarks>
public sealed record CosmosStore(string ConnectionString, string DatabaseName, string ContainerName)
{
    /// <summary>The setting holding the database name.</summary>
    public const string DatabaseSetting = "Database:Cosmos:DatabaseName";

    /// <summary>The setting holding the container name.</summary>
    public const string ContainerSetting = "Database:Cosmos:ContainerName";

    /// <summary>
    /// Reads where a Cosmos store's documents are.
    /// </summary>
    public static CosmosStore Of(DatabaseConnection database, IConfiguration configuration) =>
        new(database.ConnectionString,
            configuration[DatabaseSetting] is { Length: > 0 } databaseName ? databaseName : "Memoria",
            configuration[ContainerSetting] is { Length: > 0 } containerName ? containerName : "Domain");
}
