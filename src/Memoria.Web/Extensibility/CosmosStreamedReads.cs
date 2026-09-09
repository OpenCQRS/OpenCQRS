using Memoria.EventSourcing.Store.Cosmos.Documents;
using Microsoft.Azure.Cosmos;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Answers the pages' two questions from a Cosmos DB store, through the Cosmos SDK.
/// </summary>
/// <param name="client">The account the store lives in.</param>
/// <param name="databaseName">The database the container is in.</param>
/// <param name="containerName">The container the store writes into.</param>
/// <remarks>
/// Not Entity Framework Core. The Cosmos store writes its documents with this SDK and owns their
/// shape — one container discriminated by <c>documentType</c>, partitioned by <c>streamId</c> — so
/// the tool reads them the same way rather than describing that shape a second time in a model.
/// <para>
/// Every read here crosses partitions, because the pages ask across streams rather than about one.
/// That is the cost of the question, not of how it is asked; a store read one stream at a time is
/// what <c>ICosmosDataStore</c> already offers, and it cannot answer "the newest twenty-five of
/// everything".
/// </para>
/// </remarks>
public sealed class CosmosStreamedReads(CosmosClient client, string databaseName, string containerName)
    : IStreamedReads
{
    /// <inheritdoc />
    /// <remarks>
    /// Two queries: one for how many there are and one for the page itself. The count is its own
    /// trip because the page needs a total it cannot infer from twenty-five rows, and Cosmos will
    /// not return both at once.
    /// </remarks>
    public async Task<StoredStreamEvents> Events(
        StreamedEventFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = client.GetContainer(databaseName, containerName);

            var total = await Count(container, DocumentType.Event, cancellationToken);
            var placed = InstanceQuery.Place(filter.Page, total, filter.Size);

            var (documents, notice) = await Ordered(container, filter, placed, cancellationToken);

            // The same reading the relational log goes through, so a row says the same thing
            // wherever it is met — including one whose type the uploaded assemblies no longer
            // describe, which is listed rather than dropped.
            var read = documents
                .Select(document => new StoredStreamEvent(
                    document.StreamId,
                    document.Id,
                    BoundaryEvents.Read(document.Sequence, document.EventType, document.Data,
                        document.CreatedDate)))
                .ToList();

            return new StoredStreamEvents(read, total, placed.Page, placed.TotalPages, Error: null)
            {
                OrderingNotice = notice
            };
        }
        catch (Exception exception)
        {
            return new StoredStreamEvents([], Total: 0, Page: 1, TotalPages: 1, exception.Message);
        }
    }

    /// <summary>
    /// One page of events in the fullest order the container can serve.
    /// </summary>
    /// <returns>The documents, and why the order is coarser than asked for when it is.</returns>
    /// <remarks>
    /// <para>
    /// The three keys are what the relational read orders by, and for the same reason: several
    /// events written together share a date exactly, and an order that cannot separate them would
    /// show one row on two pages and another on none.
    /// </para>
    /// <para>
    /// Cosmos will not serve an <c>ORDER BY</c> over more than one property unless a composite index
    /// covers it in that exact direction, and refuses the query outright rather than running it
    /// slowly. The store's own indexing policy ships without such an index deliberately — three were
    /// drafted and cost about 7% on every write while returning nothing to the store's own reads —
    /// so the container this tool is pointed at usually has none.
    /// </para>
    /// <para>
    /// Rather than demand an indexing change to a store this tool only reads, the refusal is taken
    /// as the answer to a question and the date alone is asked for instead, which the store's policy
    /// already indexes. The caller is told, because a coarser order is a real difference rather than
    /// an implementation detail.
    /// </para>
    /// </remarks>
    private static async Task<(List<EventDocument> Documents, string? Notice)> Ordered(
        Container container, StreamedEventFilter filter, PlacedPage placed,
        CancellationToken cancellationToken)
    {
        var direction = filter.Descending ? "DESC" : "ASC";

        try
        {
            return (await Page(container,
                    $"c.createdDate {direction}, c.streamId ASC, c.sequence {direction}",
                    filter, placed, cancellationToken),
                null);
        }
        catch (CosmosException refused) when (NeedsACompositeIndex(refused))
        {
            return (await Page(container, $"c.createdDate {direction}", filter, placed,
                    cancellationToken),
                "This container has no composite index for the full order, so these events are " +
                "ordered by date alone. Events written at the same moment may move between pages.");
        }
    }

    /// <summary>
    /// Whether Cosmos refused a query because the order it asks for has no composite index.
    /// </summary>
    /// <remarks>
    /// Read off the message because Cosmos gives no sub-status that separates this from the other
    /// ways a query is malformed, and a bad request that is not this one must still surface as the
    /// error it is rather than be retried in a coarser order.
    /// </remarks>
    private static bool NeedsACompositeIndex(CosmosException exception) =>
        exception.StatusCode is System.Net.HttpStatusCode.BadRequest &&
        exception.Message.Contains("composite index", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// One page of event documents in the given order.
    /// </summary>
    private static Task<List<EventDocument>> Page(
        Container container, string order, StreamedEventFilter filter, PlacedPage placed,
        CancellationToken cancellationToken) =>
        Read<EventDocument>(container, new QueryDefinition(
                    $"SELECT * FROM c WHERE c.documentType = @documentType ORDER BY {order} " +
                    "OFFSET @skip LIMIT @take")
                .WithParameter("@documentType", DocumentType.Event)
                .WithParameter("@skip", placed.Skip)
                .WithParameter("@take", filter.Size),
            cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Not read from Cosmos yet. Answered as an error rather than thrown, so the two pages that ask
    /// say what is missing where the rows would be instead of failing the request.
    /// </remarks>
    public Task<StoredStreamSnapshots> Snapshots(
        StreamedSnapshotFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(new StoredStreamSnapshots([], Total: 0, Page: 1, TotalPages: 1,
            Error: "Stored models are not read from a Cosmos store yet. The events they were " +
                   "folded from are."));

    /// <summary>
    /// How many documents of one kind the container holds.
    /// </summary>
    private static async Task<int> Count(
        Container container, string documentType, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = @documentType")
            .WithParameter("@documentType", documentType);

        var counted = await Read<int>(container, query, cancellationToken);

        return counted.FirstOrDefault();
    }

    /// <summary>
    /// Drains one query across every partition it reaches.
    /// </summary>
    private static async Task<List<T>> Read<T>(
        Container container, QueryDefinition query, CancellationToken cancellationToken)
    {
        var results = new List<T>();

        using var iterator = container.GetItemQueryIterator<T>(query);

        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }
}
