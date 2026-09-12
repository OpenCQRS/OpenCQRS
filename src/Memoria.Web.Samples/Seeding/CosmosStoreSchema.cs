using System.Net;
using Memoria.EventSourcing.Store.Cosmos;
using Memoria.EventSourcing.Store.Cosmos.Configuration;
using Memoria.Web.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Makes sure the Cosmos database and container are there before anything tries to write to them.
/// </summary>
/// <remarks>
/// The Cosmos counterpart of <see cref="StoreSchema"/>, and a much shorter one: a container has no
/// schema to install, so there is nothing to ask about table by table. What the store does need is a
/// partition key and the indexing policy its queries are built for, and both of those belong to the
/// store rather than to the samples — so <see cref="CosmosSetup"/> is asked rather than a container
/// being created here with a partition key written out a second time.
/// </remarks>
public static class CosmosStoreSchema
{
    /// <summary>
    /// Creates the database and the container if they are not there.
    /// </summary>
    /// <param name="provider">The client, and the container it resolves to.</param>
    /// <param name="store">The database and container names to create under.</param>
    /// <param name="cancellationToken">A token to cancel the read with.</param>
    /// <returns>Whether anything was created.</returns>
    public static async Task<bool> Ensure(
        CosmosClientProvider provider, CosmosStore store, CancellationToken cancellationToken = default)
    {
        var existed = await Exists(provider, cancellationToken);

        // Only the two names are given. The endpoint and the key on these options are what the store
        // would build a client of its own from, and it has no reason to: the provider handed over
        // already holds the client this run reads and writes with.
        var setup = new CosmosSetup(
            Options.Create(new CosmosOptions
            {
                DatabaseName = store.DatabaseName,
                ContainerName = store.ContainerName
            }),
            provider);

        await setup.CreateDatabaseAndContainerIfNotExist();

        return !existed;
    }

    /// <summary>
    /// Whether the container is already there.
    /// </summary>
    /// <remarks>
    /// Asked before creating rather than read off the creation, because creating is idempotent and
    /// says nothing about which of the two calls it made. A missing database answers the same way a
    /// missing container does, which is all this needs to know. Anything else — an account that is
    /// not running, a key that is refused — is left to reach the caller, where it is reported as a
    /// store that could not be reached.
    /// </remarks>
    private static async Task<bool> Exists(CosmosClientProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            await provider.Container.ReadContainerAsync(cancellationToken: cancellationToken);

            return true;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
