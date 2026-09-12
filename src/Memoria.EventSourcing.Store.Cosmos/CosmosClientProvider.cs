using Memoria.EventSourcing.Store.Cosmos.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Memoria.EventSourcing.Store.Cosmos;

/// <summary>
/// Owns the single <see cref="CosmosClient"/> the store uses, and the <see cref="Container"/>
/// resolved from <see cref="CosmosOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="CosmosClient"/> is meant to live for the lifetime of the application. Each instance
/// performs its own account discovery, builds its own routing map and — in
/// <see cref="ConnectionMode.Direct"/>, the Memoria default — opens its own connections to every
/// replica it touches. None of that is shared between instances, so creating one per request pays
/// the warm-up cost again every time and disposing it throws the connections away.
/// </para>
/// <para>
/// <c>AddMemoriaCosmos</c> registers this type as a singleton, which is what makes the client a
/// singleton. Because the client is built once, changes to <see cref="CosmosOptions"/> after the
/// first resolution have no effect on the connection it holds.
/// </para>
/// <para>
/// A client may instead be supplied and owned externally through the
/// <see cref="CosmosClientProvider(CosmosClient, string, string)"/> constructor. That is for a host
/// that already registers its own <see cref="CosmosClient"/> singleton and wants the store to reuse
/// it; in that case this provider does not dispose the client, leaving its lifetime to the owner.
/// </para>
/// </remarks>
public sealed class CosmosClientProvider : IDisposable
{
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosClientProvider"/> class that builds and
    /// owns its own <see cref="CosmosClient"/> from the given options.
    /// </summary>
    /// <param name="options">The Cosmos DB configuration options.</param>
    public CosmosClientProvider(IOptions<CosmosOptions> options)
    {
        var cosmosOptions = options.Value;

        Client = new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.AuthKey, cosmosOptions.ClientOptions);
        Container = Client.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.ContainerName);
        _ownsClient = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosClientProvider"/> class that wraps an
    /// externally-owned <see cref="CosmosClient"/>. The client is not disposed by this provider.
    /// </summary>
    /// <param name="client">The shared client, owned by whoever created it.</param>
    /// <param name="databaseName">The database the container is in.</param>
    /// <param name="containerName">The container the store writes into.</param>
    public CosmosClientProvider(CosmosClient client, string databaseName, string containerName)
    {
        Client = client;
        Container = client.GetContainer(databaseName, containerName);
        _ownsClient = false;
    }

    /// <summary>
    /// Gets the shared Cosmos DB client. Do not dispose it: it belongs to this provider, which the
    /// container disposes when the application shuts down.
    /// </summary>
    public CosmosClient Client { get; }

    /// <summary>
    /// Gets the container holding events, aggregates, aggregate-event links and projections.
    /// </summary>
    public Container Container { get; }

    /// <summary>
    /// Disposes the shared Cosmos DB client, but only when this provider owns it. Called by the
    /// dependency injection container when the application shuts down. When the client was supplied
    /// externally its lifetime belongs to its owner, so this is a no-op for the client.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            Client.Dispose();
        }
    }
}
