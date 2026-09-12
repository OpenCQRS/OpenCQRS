namespace Memoria.Web.Extensibility;

/// <summary>
/// Which registered type a page of stored rows was narrowed to: all of them, one of them, or
/// neither.
/// </summary>
/// <param name="Chosen">The type chosen, or null when none was or the name reached nothing.</param>
/// <param name="Unknown">Whether a name was asked for that is not among the registered types.</param>
/// <remarks>
/// Asking for nothing and asking for something that is not there are two different questions with
/// two different answers, and both leave <see cref="Chosen"/> null — so which was asked is kept
/// beside it rather than inferred from it. Answering an unknown name with everything stored would
/// look like the filter had been applied, and read as the store holding rows it does not.
/// <para>
/// Nothing here is about events, aggregates or projections in particular: the key a page narrows by
/// is read off whichever of the three attributes the chosen type carries, so one choice serves every
/// page that lists what the store wrote under a type. What the store does with that key is the
/// reader's business — see <see cref="AppendedEvents"/>, <see cref="StreamedEvents"/> and
/// <see cref="StreamedSnapshots"/>.
/// </para>
/// </remarks>
public sealed record TypeChoice(Type? Chosen, bool Unknown)
{
    /// <summary>
    /// Reads the choice out of the name in the address.
    /// </summary>
    /// <param name="types">
    /// The types the page lists, which is what the name is matched against — so a name arriving in a
    /// query string can only ever reach a type this application already knows about, and only one
    /// the page's own model uses.
    /// </param>
    /// <param name="asked">The full name asked for, or null when everything was.</param>
    public static TypeChoice Of(IReadOnlyList<Type> types, string? asked) =>
        string.IsNullOrWhiteSpace(asked)
            ? new TypeChoice(Chosen: null, Unknown: false)
            : DomainTypeDescriber.Select(types, asked) is { } found
                ? new TypeChoice(found, Unknown: false)
                : new TypeChoice(Chosen: null, Unknown: true);

    /// <summary>Gets whether everything stored is being read.</summary>
    public bool IsAll => Chosen is null && !Unknown;

    /// <summary>
    /// Gets the key the store wrote the chosen type under, as <c>name:version</c>, or null when
    /// there is no single type to narrow to.
    /// </summary>
    /// <remarks>
    /// Read off whichever of <c>[EventType]</c>, <c>[AggregateType]</c> and <c>[ProjectionType]</c>
    /// the type carries, which is what lets one choice answer for all three: a page lists types of
    /// one kind, so the attribute found is the one that kind carries.
    /// </remarks>
    public string? Key => Chosen is null ? null : DomainTypeDescriber.BindingOf(Chosen)?.Key;

    /// <summary>
    /// Gets whether the chosen type carries none of those attributes, so the store was never able to
    /// write one of it.
    /// </summary>
    public bool Unbound => Chosen is not null && Key is null;

    /// <summary>
    /// Gets whether there are no rows to look for. Null narrows to nothing rather than to
    /// everything, so a page asks this before it asks the store — the alternative is showing
    /// everything stored under the name of a type it holds none of.
    /// </summary>
    public bool ShowsNothing => Unknown || Unbound;

    /// <summary>
    /// Gets what is being read, for the line that counts what a page holds. A chosen type is named
    /// as the store names it, falling back to the class when it is bound by nothing.
    /// </summary>
    /// <remarks>
    /// The two answers for an unchosen type are deliberately plain. Every page here says what its
    /// rows are in its own words and only asks for this once a type has been chosen, so these are
    /// what is left to say when none has — not a name any page is expected to print.
    /// </remarks>
    public string Name =>
        Chosen is null
            ? Unknown ? "Types" : "All types"
            : DomainTypeDescriber.LabelOf(Chosen);
}
