namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads stored rows back into the types that made them: the model a row's key names, and the
/// identifier that addresses the boundary it was folded under.
/// </summary>
/// <remarks>
/// What a list of every model's rows needs and a list of one identifier's does not. There the
/// identifier is chosen, so its shape unfolds every row; here each row carries its own key and its
/// own boundary, and the types they name have to be found before the row can be named or led from.
/// <para>
/// Built for one page of rows and kept for the length of it. Resolving means building identifiers
/// and rendering their boundaries, and a page asks about the same few types over and over.
/// </para>
/// </remarks>
public sealed class StoredModels(IReadOnlyList<Type> models, IReadOnlyList<Type> identifiers)
{
    private readonly Dictionary<string, Type?> _byKey = new(StringComparer.Ordinal);

    private readonly Dictionary<Type, IReadOnlyList<Type>> _addressing = [];

    /// <summary>
    /// Reads one stored row.
    /// </summary>
    /// <param name="modelType">The model's binding key, as <c>name:version</c>.</param>
    /// <param name="boundary">The boundary it was folded under, in canonical form.</param>
    public StoredModel Of(string modelType, string boundary)
    {
        if (Model(modelType) is not { } model)
        {
            return new StoredModel(Model: null, Identifier: null, Values: EmptyValues);
        }

        foreach (var identifier in Addressing(model))
        {
            if (IdentifierShape.Of(identifier)?.ValuesFromBoundary(boundary) is not { } values)
            {
                continue;
            }

            if (Renders(identifier, values, boundary))
            {
                return new StoredModel(model, identifier, values);
            }
        }

        return new StoredModel(model, Identifier: null, Values: EmptyValues);
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyValues =
        new Dictionary<string, string>();

    /// <summary>
    /// The model written under one key, read off the attributes rather than through the framework's
    /// own lookup: the uploaded types are this page's world, and a key naming something outside it
    /// names nothing here.
    /// </summary>
    private Type? Model(string modelType)
    {
        if (_byKey.TryGetValue(modelType, out var known))
        {
            return known;
        }

        var found = models.FirstOrDefault(model =>
            DomainTypeDescriber.BindingOf(model)?.Key == modelType);

        _byKey[modelType] = found;

        return found;
    }

    private IReadOnlyList<Type> Addressing(Type model)
    {
        if (_addressing.TryGetValue(model, out var known))
        {
            return known;
        }

        var found = DomainTypeDescriber.Describe(model, identifiers).Identifiers;

        _addressing[model] = found;

        return found;
    }

    /// <summary>
    /// Whether an identifier built from these values renders back to this very boundary.
    /// </summary>
    /// <remarks>
    /// The tags alone are not enough to tell identifiers apart. A narrower one carries tags a wider
    /// boundary also has — <c>sample:abc</c> is inside <c>sample:abc,label:x</c> — and two of the
    /// same tags differ only in how they combine, a union selecting events inside either tag and an
    /// intersection only those inside both. Building it and rendering it is what the row was written
    /// by, so it is what says whether this identifier addresses it.
    /// </remarks>
    private static bool Renders(
        Type identifier, IReadOnlyDictionary<string, string> values, string boundary)
    {
        var created = IdentifierFactory.Create(identifier,
            values.ToDictionary(value => value.Key, value => (string?)value.Value));

        return DcbModels.BoundaryOf(created.Instance)?.ToString() == boundary;
    }
}

/// <summary>What one stored row turned out to be.</summary>
/// <param name="Model">The model its key names, or null when no uploaded type carries that key.</param>
/// <param name="Identifier">The identifier addressing its boundary, or null when none does.</param>
/// <param name="Values">That identifier's values, by the parameter each belongs to.</param>
public sealed record StoredModel(
    Type? Model,
    Type? Identifier,
    IReadOnlyDictionary<string, string> Values)
{
    /// <summary>
    /// Gets whether there is a page about this row to lead to. A row is worth listing either way —
    /// the boundary, the version and the dates are the store's own facts and hold whatever the
    /// uploaded types turn out to say — but only an addressed one can be opened.
    /// </summary>
    public bool IsAddressable => Identifier is not null;
}
