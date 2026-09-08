namespace Memoria.Web.Extensibility;

/// <summary>
/// Which stored models a data page was asked for: every one of its kind, one type of them, or one
/// type addressed one way.
/// </summary>
/// <param name="Model">The model chosen, or null when none was or the name reached nothing.</param>
/// <param name="Unknown">Whether a model was asked for that is not among the registered types.</param>
/// <param name="Identifier">The identifier chosen, or null when every one of the model's is.</param>
/// <param name="Identifiers">The identifiers addressing the chosen model, which is what the second
/// dropdown offers. Empty until a model is chosen.</param>
/// <remarks>
/// Two choices rather than one, and the second belongs to the first: an identifier addresses one
/// model, so it only means anything once a model is chosen. What that costs is that the two travel
/// in one form, and changing the model submits the identifier chosen for the model before it — so a
/// stale identifier widens to all of the new model's rather than being an error. A stale model is
/// not the same thing and does not widen: nothing the reader can do here produces one, so a name
/// reaching nothing is a link gone bad and says so.
/// </remarks>
public sealed record ModelChoice(
    Type? Model,
    bool Unknown,
    Type? Identifier,
    IReadOnlyList<Type> Identifiers)
{
    /// <summary>
    /// Reads the choice out of the two names in the address.
    /// </summary>
    /// <param name="models">The models the page lists, which is what the name is matched against —
    /// so a name arriving in a query string can only ever reach a type this application already
    /// knows about.</param>
    /// <param name="identifiers">The identifier types to look through for ones addressing it.</param>
    /// <param name="model">The model's full name, or null when everything was asked for.</param>
    /// <param name="identifier">The identifier's full name, or null when all of them were.</param>
    public static ModelChoice Of(
        IReadOnlyList<Type> models,
        IReadOnlyList<Type> identifiers,
        string? model,
        string? identifier)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            // An identifier without a model is a way into nothing in particular, so it is dropped
            // rather than carried as half a choice.
            return new ModelChoice(Model: null, Unknown: false, Identifier: null, Identifiers: []);
        }

        if (DomainTypeDescriber.Select(models, model) is not { } found)
        {
            return new ModelChoice(Model: null, Unknown: true, Identifier: null, Identifiers: []);
        }

        var addressing = DomainTypeDescriber.Describe(found, identifiers).Identifiers;

        // Matched against the ones addressing this model rather than against every identifier, so
        // one belonging to another model widens the table instead of narrowing it to nothing.
        var chosen = addressing.FirstOrDefault(candidate => candidate.FullName == identifier);

        return new ModelChoice(found, Unknown: false, chosen, addressing);
    }

    /// <summary>Gets whether every model of the kind is being listed.</summary>
    public bool IsAllModels => Model is null && !Unknown;

    /// <summary>Gets whether every identifier of the chosen model is.</summary>
    public bool IsAllIdentifiers => Identifier is null;

    /// <summary>
    /// Gets the key the store wrote the chosen model's snapshots under, as <c>name:version</c>, or
    /// null when there is no single model to narrow to.
    /// </summary>
    /// <remarks>
    /// Read off the attribute rather than through the framework's own lookup, which throws for a
    /// type carrying none — a page listing what was uploaded has to be able to draw a type that
    /// cannot be stored, and say why, rather than fail on it.
    /// </remarks>
    public string? Key => Model is null ? null : DomainTypeDescriber.BindingOf(Model)?.Key;

    /// <summary>
    /// Gets whether the chosen model carries no type attribute, so the store was never able to
    /// write a snapshot of it.
    /// </summary>
    public bool Unbound => Model is not null && Key is null;

    /// <summary>
    /// Gets which tags the chosen identifier's values live in, or null when no single identifier is
    /// chosen or its values cannot be read back out of a stored boundary.
    /// </summary>
    public IdentifierShape? Shape => Identifier is null ? null : IdentifierShape.Of(Identifier);

    /// <summary>
    /// Gets whether the chosen identifier takes a value its boundary never mentions, so which of
    /// these exist cannot be worked out from the stored tags.
    /// </summary>
    public bool Shapeless => Identifier is not null && Shape is null;

    /// <summary>
    /// Gets whether there are no rows to look for. Each of these is a different thing to say and the
    /// page says which; what they share is that asking the store would answer the wrong question —
    /// a null key and a null shape both narrow to everything rather than to nothing.
    /// </summary>
    public bool ShowsNothing => Unknown || Unbound || Shapeless;
}
