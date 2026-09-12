using Memoria.EventSourcing.Store.Cosmos;
using Memoria.Web.Data;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// A Cosmos store, which holds the streamed model only and has no context to reach it through.
/// </summary>
/// <remarks>
/// The whole reason this exists. The store the samples are written to used to be assumed relational,
/// so a Cosmos connection string reached <see cref="DatabaseConnection.Apply"/> — which has no
/// provider to give for Cosmos and says so — and the run ended before it had asked anything. The
/// writing itself needed nothing new: the sample data goes through <c>IDomainService</c>, and the
/// Cosmos store has had one of those all along.
/// </remarks>
public sealed class CosmosSampleStore(CosmosClientProvider provider, CosmosStore store) : ISampleStore
{
    /// <inheritdoc />
    /// <remarks>
    /// Both names, because a Cosmos account holds many containers and the database alone would not
    /// say which one the run filled.
    /// </remarks>
    public string Where => $"{store.DatabaseName}/{store.ContainerName}";

    /// <inheritdoc />
    /// <remarks>
    /// The streamed model alone. There is no Cosmos dynamic consistency boundary store — the model
    /// would not build on that provider even if there were — so the DCB half is never offered, which
    /// is the same answer the web tool gives its own pages through <c>StoreCapabilities</c>.
    /// </remarks>
    public SampleDataScope Holds => SampleDataScope.Streamed;

    /// <inheritdoc />
    public Task<bool> Install(CancellationToken cancellationToken = default) =>
        CosmosStoreSchema.Ensure(provider, store, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The scope is not consulted, because there is only one thing in the container to delete. A run
    /// can never arrive here asking for the DCB half: <see cref="Holds"/> is what the question was
    /// narrowed to.
    /// </remarks>
    public Task<IReadOnlyList<(string Table, int Rows)>> Erase(
        SampleDataScope scope, CancellationToken cancellationToken = default) =>
        CosmosSampleDataEraser.EraseStreamed(provider.Container, cancellationToken);
}
