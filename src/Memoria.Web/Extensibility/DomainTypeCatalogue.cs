namespace Memoria.Web.Extensibility;

/// <summary>
/// The domain types found in the uploaded assemblies, kept for the pages that list them.
/// </summary>
/// <remarks>
/// Aggregate and projection identifiers are here because nothing in the framework binds them:
/// <c>AddMemoriaEventSourcing</c> and <c>AddMemoriaDcb</c> bind only the attributed events,
/// aggregates and projections, and an identifier carries no attribute.
/// </remarks>
public sealed record DomainTypeCatalogue
{
    /// <summary>An empty catalogue, for before anything has been uploaded.</summary>
    public static readonly DomainTypeCatalogue Empty = new();

    /// <summary>
    /// The streams the streamed models are folded from.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="StreamedAggregateIds"/> because a stream is not an aggregate's
    /// identifier: several aggregates may share one stream, so the two are separate contracts and
    /// a type declaring one of them says nothing about the other.
    /// </remarks>
    public IReadOnlyList<Type> StreamedStreamIds { get; init; } = [];

    public IReadOnlyList<Type> StreamedAggregates { get; init; } = [];

    public IReadOnlyList<Type> StreamedAggregateIds { get; init; } = [];

    public IReadOnlyList<Type> StreamedProjections { get; init; } = [];

    public IReadOnlyList<Type> StreamedProjectionIds { get; init; } = [];

    public IReadOnlyList<Type> DcbAggregates { get; init; } = [];

    public IReadOnlyList<Type> DcbAggregateIds { get; init; } = [];

    public IReadOnlyList<Type> DcbProjections { get; init; } = [];

    public IReadOnlyList<Type> DcbProjectionIds { get; init; } = [];

    /// <summary>
    /// Every event found, which is the set the framework binds: an event is the same event
    /// whichever model appends it, so there is one map and one list of them.
    /// </summary>
    public IReadOnlyList<Type> Events { get; init; } = [];

    /// <summary>
    /// The events some streamed model applies.
    /// </summary>
    /// <remarks>
    /// A subset of <see cref="Events"/>, kept apart from it because a page about one consistency
    /// model has no use for an event nothing in that model folds. Which is not the same as an event
    /// nothing appends: this is read off the models' own filters, and an event no model applies may
    /// still be written to the log for something else to read.
    /// </remarks>
    public IReadOnlyList<Type> StreamedEvents { get; init; } = [];

    /// <summary>The events some DCB model applies. See <see cref="StreamedEvents"/>.</summary>
    public IReadOnlyList<Type> DcbEvents { get; init; } = [];

    /// <summary>
    /// The uploaded assemblies the types were read from, each with the file it was loaded from.
    /// </summary>
    /// <remarks>
    /// Only the uploads: the application's own assembly is scanned alongside them but was not
    /// brought by any file, and the one question this answers is which file brought what.
    /// </remarks>
    public IReadOnlyList<LoadedAssembly> Assemblies { get; init; } = [];

    /// <summary>
    /// What went wrong while loading, one line per assembly that could not be read. Held rather
    /// than thrown so a bad upload leaves the application running and able to say so.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Gets when this catalogue was built, or null before the first reload.</summary>
    public DateTime? ReloadedUtc { get; init; }

    /// <summary>
    /// The types registered from one assembly file, grouped under the kind of thing each is.
    /// </summary>
    /// <param name="fileName">The file's name in the library, as the archive that holds it lists it.</param>
    /// <returns>
    /// One group per kind something was registered under, in the order the Types tab counts them
    /// — the streamed model's kinds, then the DCB model's — and nothing for a file that was not
    /// loaded or declared none of them. The types within a group keep the order they are
    /// catalogued in.
    /// </returns>
    /// <remarks>
    /// The events are split the way the event pages split them: each model's group holds the
    /// events that model applies, so an event both apply is under both, and one no model applies
    /// is under neither — the same answer those pages give.
    /// <para>
    /// Compared without regard to case: the name comes from an archive entry on one side and a
    /// directory listing on the other, and neither is somewhere a difference of case is a
    /// different file.
    /// </para>
    /// </remarks>
    public IReadOnlyList<RegisteredKind> RegisteredFrom(string fileName)
    {
        var assembly = Assemblies
            .FirstOrDefault(loaded => string.Equals(loaded.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            ?.Assembly;

        if (assembly is null)
        {
            return [];
        }

        IReadOnlyList<(string Label, IReadOnlyList<Type> Types)> kinds =
        [
            ("Streamed events", StreamedEvents),
            ("Streamed aggregates", StreamedAggregates),
            ("Streamed aggregate ids", StreamedAggregateIds),
            ("Streamed projections", StreamedProjections),
            ("Streamed projection ids", StreamedProjectionIds),
            ("Streams", StreamedStreamIds),
            ("DCB events", DcbEvents),
            ("DCB aggregates", DcbAggregates),
            ("DCB aggregate ids", DcbAggregateIds),
            ("DCB projections", DcbProjections),
            ("DCB projection ids", DcbProjectionIds)
        ];

        return kinds
            .Select(kind => new RegisteredKind(kind.Label,
                kind.Types.Where(type => type.Assembly == assembly).ToList()))
            .Where(kind => kind.Types.Count > 0)
            .ToList();
    }

    /// <summary>Gets the number of domain types found, identifiers included.</summary>
    /// <remarks>
    /// The events counted are the whole set. <see cref="StreamedEvents"/> and <see cref="DcbEvents"/>
    /// are two views of that set rather than more types, and counting them here would report one
    /// event two or three times.
    /// </remarks>
    public int Count =>
        StreamedStreamIds.Count +
        StreamedAggregates.Count + StreamedAggregateIds.Count +
        StreamedProjections.Count + StreamedProjectionIds.Count +
        DcbAggregates.Count + DcbAggregateIds.Count +
        DcbProjections.Count + DcbProjectionIds.Count +
        Events.Count;
}

/// <summary>
/// The types one file registered under one kind of thing.
/// </summary>
/// <param name="Label">The kind, in the words the Types tab counts it in.</param>
/// <param name="Types">The types of that kind the file's assembly declares.</param>
public sealed record RegisteredKind(string Label, IReadOnlyList<Type> Types);
