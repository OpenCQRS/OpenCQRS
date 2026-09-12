namespace Memoria.Web.Extensibility;

/// <summary>
/// Which of the types that make ids a page was narrowed to — a stream, or the identifier of a model
/// in one: all of them, one of them, or neither.
/// </summary>
/// <param name="Chosen">The type chosen, or null when none was or the name reached nothing.</param>
/// <param name="Unknown">Whether a name was asked for that is not among the registered types.</param>
/// <remarks>
/// The same shape as <see cref="TypeChoice"/> and for the same reason: asking for nothing and asking
/// for something that is not there are two different questions with two different answers, and both
/// leave <see cref="Chosen"/> null — so which was asked is kept beside it rather than inferred from
/// it. Answering an unknown name with everything stored would look like the filter had been applied,
/// and read as that stream or identifier holding rows written elsewhere.
/// <para>
/// The difference from <see cref="TypeChoice"/> is what a chosen type narrows by. A model's type is
/// one key the store wrote, so it is matched exactly; a stream or an identifier is as many ids as it
/// has been given values — a customer apiece — so it is matched by the shape of the ids it makes.
/// That also settles what a dropdown can offer: the ids are a list as long as the store is wide, and
/// the types are the thing there are few enough of to choose from.
/// </para>
/// </remarks>
public sealed record IdChoice(Type? Chosen, bool Unknown)
{
    /// <summary>Everything, which is what a page shows before anything is chosen.</summary>
    public static readonly IdChoice All = new(Chosen: null, Unknown: false);

    /// <summary>
    /// Reads the choice out of the name in the address.
    /// </summary>
    /// <param name="named">
    /// The types the page offers, which is what the name is matched against — so a name arriving in
    /// a query string can only ever reach a type this application already knows about.
    /// </param>
    /// <param name="asked">The full name asked for, or null when everything was.</param>
    public static IdChoice Of(IReadOnlyList<Type> named, string? asked) =>
        string.IsNullOrWhiteSpace(asked)
            ? All
            : DomainTypeDescriber.Select(named, asked) is { } found
                ? new IdChoice(found, Unknown: false)
                : new IdChoice(Chosen: null, Unknown: true);

    /// <summary>Gets whether everything is being read.</summary>
    public bool IsAll => Chosen is null && !Unknown;

    /// <summary>
    /// Gets the pattern the ids of the chosen type match, or null when there is no single type to
    /// narrow to or its pattern cannot be worked out.
    /// </summary>
    public string? Pattern => Chosen is null ? null : IdShape.Of(Chosen)?.Pattern;

    /// <summary>
    /// Gets whether the chosen type is one whose ids cannot be recognised, so there is no way to ask
    /// the store for them. The counterpart of <see cref="TypeChoice.Unbound"/>: registered, listed,
    /// and impossible to narrow by.
    /// </summary>
    public bool Unshaped => Chosen is not null && Pattern is null;

    /// <summary>
    /// Gets whether there are no rows to look for. Null narrows to everything rather than to
    /// nothing, so a page asks this before it asks the store — the alternative is showing everything
    /// stored under the name of one stream or one identifier.
    /// </summary>
    public bool ShowsNothing => Unknown || Unshaped;

    /// <summary>
    /// Gets what is being read, for the line that counts what a page holds. The type as the pages
    /// that list it name it, rather than the pattern it happens to match by: the pattern is how the
    /// question is asked, not what was asked for.
    /// </summary>
    /// <remarks>
    /// The two answers for an unchosen type are deliberately plain, as <see cref="TypeChoice.Name"/>
    /// is: every page says what its rows are in its own words and only asks for this once a type has
    /// been chosen.
    /// </remarks>
    public string Name =>
        Chosen is null
            ? Unknown ? "Types" : "All types"
            : DomainTypeDescriber.LabelOf(Chosen);
}
