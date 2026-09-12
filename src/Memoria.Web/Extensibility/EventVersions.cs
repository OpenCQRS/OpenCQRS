namespace Memoria.Web.Extensibility;

/// <summary>
/// Which version of a model each row on its events tab produced: what the compare column names on
/// either side of a row.
/// </summary>
/// <remarks>
/// A model's versions count its own events from one, so a row's version is its place in the
/// model's history rather than its sequence in the stream — on a stream several models share, the
/// sequence counts the others' events too, and a reader following one model wants the number that
/// climbs by one on every row.
/// <para>
/// A table showing the whole history knows every row's place from the page it is on, its order and
/// the total, and asks the store nothing. A table narrowed to some of the history knows it for none
/// of them, because the rows between two shown may be hidden rather than another model's; each is
/// placed by asking how many of the model's events sit below it, one count per row.
/// </para>
/// </remarks>
public static class EventVersions
{
    /// <summary>
    /// Numbers the rows of one page of a model's history.
    /// </summary>
    /// <param name="reads">The tool's own reads.</param>
    /// <param name="model">
    /// How the table reads the model's history: its stream, and the types and properties that make
    /// an event this model's. Its order, page and text are not read here.
    /// </param>
    /// <param name="page">The page as read: its rows, and the total and place it was read at.</param>
    /// <param name="descending">Whether the page's rows run newest first.</param>
    /// <param name="narrowed">Whether the page shows only some of the history.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// Each row's sequence to the version it produced. A row whose count the store refused is left
    /// out, so a page can draw the rows it can and say nothing for the rest.
    /// </returns>
    public static async Task<IReadOnlyDictionary<long, int>> Of(
        IStreamedReads reads,
        StreamedEventFilter model,
        StoredStreamEvents page,
        bool descending,
        bool narrowed,
        CancellationToken cancellationToken = default)
    {
        var versions = new Dictionary<long, int>();

        for (var index = 0; index < page.Events.Count; index++)
        {
            var position = page.Events[index].Event.Position;

            if (!narrowed)
            {
                // Where the row sits in the whole history, counted from the oldest: the rows on the
                // pages before this one — a page's worth each, whatever this page holds, since only
                // the last runs short — then this row's place on it. Or, newest first, the same
                // counted back from the total.
                var offset = (page.Page - 1) * model.Size + index;

                versions[position] = descending ? page.Total - offset : offset + 1;

                continue;
            }

            var below = await reads.Events(new StreamedEventFilter(
                model.StreamPattern,
                EventType: null,
                Text: null,
                Descending: true,
                Page: 1,
                Size: 1)
            {
                EventTypes = model.EventTypes,
                Properties = model.Properties,
                BeforeSequence = position
            }, cancellationToken);

            if (below.Error is not null)
            {
                continue;
            }

            versions[position] = below.Total + 1;
        }

        return versions;
    }
}
