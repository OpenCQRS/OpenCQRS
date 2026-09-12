namespace Memoria.Web.Extensibility;

/// <summary>
/// How far a stored snapshot's version has fallen behind the history it was folded from.
/// </summary>
/// <param name="Version">The version the stored row carries.</param>
/// <param name="Events">How many events the model is folded from.</param>
/// <remarks>
/// A version counts the events a model applied, so a row written before later events were appended
/// reads lower than the history it belongs to — the snapshot is still a valid answer to what the
/// model was, and no longer an answer to what it is. That is an ordinary state of an event-sourced
/// store rather than a fault, which is why this is said beside the version as a caution and not as
/// an error.
/// <para>
/// A lag at all is what there is to say, and it is only said when there is one: <see cref="Of"/>
/// answers with nothing for a row that has folded everything there is to fold, which is what lets a
/// page ask the question and draw nothing for the ordinary case.
/// </para>
/// </remarks>
public sealed record SnapshotLag(int Version, int Events)
{
    /// <summary>
    /// Works out whether the row is behind, or answers with nothing when it is not.
    /// </summary>
    /// <param name="version">The version the stored row carries.</param>
    /// <param name="events">
    /// How many events the model is folded from, or null when they could not be counted.
    /// </param>
    /// <remarks>
    /// Null and zero are different answers. Counting the history is a read of its own and it can
    /// fail, and a page that could not count has no grounds to say a row is behind it — whereas a
    /// history that counted zero is a row nothing has happened since, which is as up to date as a
    /// row gets.
    /// <para>
    /// A version above the count answers with nothing too. It is reachable rather than impossible: a
    /// model's <c>Apply</c> may decline an event its type filter let through, so a fold can count
    /// more events than it raised the version by, and a warning that the row was behind by a
    /// negative number of events would be worse than no warning at all.
    /// </para>
    /// </remarks>
    public static SnapshotLag? Of(int version, int? events) =>
        events is { } counted && counted > version ? new SnapshotLag(version, counted) : null;

    /// <summary>Gets how many of the model's events the stored version has not folded in.</summary>
    public int Behind => Events - Version;

    /// <summary>
    /// Gets the warning itself: how far behind the row is, and what would bring it up to date.
    /// </summary>
    /// <remarks>
    /// One event is written as a word and the sentence agrees with it. This is read as prose beside
    /// the number it is about, and a digit repeated there with "events" after it would read as the
    /// page having failed to say something rather than as the thing it says.
    /// </remarks>
    public string Text => Behind == 1
        ? "Behind its history by one event. Updating this snapshot would fold it in."
        : $"Behind its history by {Behind} events. Updating this snapshot would fold them in.";
}
