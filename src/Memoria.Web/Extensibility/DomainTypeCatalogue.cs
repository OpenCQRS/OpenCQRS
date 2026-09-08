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
    /// What went wrong while loading, one line per assembly that could not be read. Held rather
    /// than thrown so a bad upload leaves the application running and able to say so.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Gets when this catalogue was built, or null before the first reload.</summary>
    public DateTime? ReloadedUtc { get; init; }

    /// <summary>Gets the number of domain types found, identifiers included.</summary>
    /// <remarks>
    /// The events counted are the whole set. <see cref="StreamedEvents"/> and <see cref="DcbEvents"/>
    /// are two views of that set rather than more types, and counting them here would report one
    /// event two or three times.
    /// </remarks>
    public int Count =>
        StreamedAggregates.Count + StreamedAggregateIds.Count +
        StreamedProjections.Count + StreamedProjectionIds.Count +
        DcbAggregates.Count + DcbAggregateIds.Count +
        DcbProjections.Count + DcbProjectionIds.Count +
        Events.Count;
}
