using System.Net;
using Memoria.EventSourcing.Store.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Empties the container, so a run can start from nothing.
/// </summary>
/// <remarks>
/// The Cosmos counterpart of <see cref="SampleDataEraser"/>, and there for the same reason: an event
/// store is a log and nothing in the domain asks for one to be forgotten, so this is a sample store
/// being reset rather than an operation the framework should offer.
/// <para>
/// Only the streamed documents exist to delete. There is no Cosmos dynamic consistency boundary
/// store, which is why this has no counterpart to <c>EraseDcb</c>.
/// </para>
/// </remarks>
public static class CosmosSampleDataEraser
{
    /// <summary>
    /// Deletes every document the streamed store wrote, counted by the kind each one is.
    /// </summary>
    /// <param name="container">The container the store writes into.</param>
    /// <param name="cancellationToken">A token to cancel the reads and the deletions with.</param>
    /// <returns>How many documents of each kind went.</returns>
    /// <remarks>
    /// Read in full before anything is deleted. Paging a query while deleting from underneath it is
    /// how rows get skipped, and a half-emptied store is worse than one that was not emptied at all.
    /// Only the three properties a deletion needs are read, so the documents' own payloads never come
    /// over the wire.
    /// </remarks>
    public static async Task<IReadOnlyList<(string Table, int Rows)>> EraseStreamed(
        Container container, CancellationToken cancellationToken = default)
    {
        var documents = await Read(container, cancellationToken);
        var deleted = new Dictionary<string, int>();

        foreach (var document in documents)
        {
            await Delete(container, document, cancellationToken);

            deleted[document.DocumentType] = deleted.GetValueOrDefault(document.DocumentType) + 1;
        }

        // Every kind the store writes is reported, at nought as well, so a run says plainly that it
        // found nothing rather than leaving a kind out and reading as though it had not looked.
        string[] known = [DocumentType.Aggregate, DocumentType.Projection, DocumentType.Event];

        var report = new List<(string Table, int Rows)>
        {
            ("Aggregate documents", deleted.GetValueOrDefault(DocumentType.Aggregate)),
            ("Projection documents", deleted.GetValueOrDefault(DocumentType.Projection)),
            ("Event documents", deleted.GetValueOrDefault(DocumentType.Event))
        };

        // Anything else in the container went too — it is being emptied — so it is reported under
        // whatever it called itself rather than deleted without appearing in the count.
        report.AddRange(deleted
            .Where(kind => !known.Contains(kind.Key))
            .Select(kind => (
                kind.Key.Length > 0 ? $"{kind.Key} documents" : "untyped documents",
                kind.Value)));

        return report;
    }

    private static async Task<List<StoredDocument>> Read(
        Container container, CancellationToken cancellationToken)
    {
        var documents = new List<StoredDocument>();

        using var iterator = container.GetItemQueryIterator<StoredDocument>(
            new QueryDefinition("SELECT c.id, c.streamId, c.documentType FROM c"));

        while (iterator.HasMoreResults)
        {
            documents.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return documents;
    }

    /// <summary>
    /// Deletes one document, by its identifier and the stream it is partitioned under.
    /// </summary>
    /// <remarks>
    /// As a stream, because there is nothing to deserialize from the response and the document that
    /// came back is already gone from the caller's point of view. A document that is no longer there
    /// is not a failure: the container is shared, and something else may have removed it between the
    /// read above and this.
    /// </remarks>
    private static async Task Delete(
        Container container, StoredDocument document, CancellationToken cancellationToken)
    {
        using var response = await container.DeleteItemStreamAsync(
            document.Id, new PartitionKey(document.StreamId), cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Could not delete document '{document.Id}': {response.StatusCode} {response.ErrorMessage}");
        }
    }

    /// <summary>
    /// The three properties a deletion needs: what to delete, where it is partitioned, and what to
    /// count it as.
    /// </summary>
    private sealed class StoredDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("streamId")]
        public string StreamId { get; set; } = null!;

        /// <summary>
        /// Which kind of document this is. Defaulted rather than required, so a document written by
        /// something other than the store is still deleted and still counted somewhere.
        /// </summary>
        [JsonProperty("documentType")]
        public string DocumentType { get; set; } = string.Empty;
    }
}
