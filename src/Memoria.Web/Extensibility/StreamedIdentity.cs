using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// The stream and the identifier one stored streamed model came out of, worked back out of the two
/// ids the store keeps.
/// </summary>
/// <param name="StreamType">
/// Which registered stream writes ids of this shape, or null when none does.
/// </param>
/// <param name="Stream">That stream built again from this row's id, or null when it could not be.</param>
/// <param name="IdentifierType">
/// Which registered identifier of this model writes ids of this shape, or null when none does.
/// </param>
/// <param name="Identifier">
/// That identifier built again from this row's own values, or null when it could not be.
/// </param>
/// <param name="Values">
/// The values that identifier was built from, by the parameter that supplied each, or null when
/// they could not be read back out of the id.
/// </param>
/// <remarks>
/// The store keeps the ids a stream and an identifier produced rather than the types that produced
/// them, so both are worked back to: whichever registered type writes ids of this shape, built again
/// from the values that id must have been made of. Two pages want the answer and each wants a
/// different half of it — the events tab narrows the history by what the identifier claims, and the
/// update tab hands the pair to the store, which folds a stream through an identifier and takes the
/// things rather than the strings.
/// <para>
/// Every step can come up empty: nothing writes ids like this, the values cannot be recovered, the
/// type will not build. Each leaves its half null rather than guessed, because an unknown claim is a
/// wider history than one model's and an unknown pair is a write that would land somewhere else.
/// </para>
/// </remarks>
public sealed record StreamedIdentity(
    Type? StreamType,
    object? Stream,
    Type? IdentifierType,
    object? Identifier,
    IReadOnlyDictionary<string, string>? Values)
{
    /// <summary>Nothing worked out yet, for a page whose address names no row to work out.</summary>
    public static readonly StreamedIdentity Unknown = new(null, null, null, null, null);

    /// <summary>
    /// Gets the properties an event must carry to be this model's, or null when there is no
    /// identifier to ask or it could not be rebuilt.
    /// </summary>
    /// <remarks>
    /// Null and empty are different answers. Empty is an identifier that folds the whole stream,
    /// which is how a model is written when it is the only one in there; null is not knowing.
    /// <para>
    /// Read off the rebuilt identifier rather than off its type, because the values matter: the
    /// shape knows which properties are claimed and only an identifier built from this row's own
    /// values knows what they have to equal.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Claim => Identifier switch
    {
        IAggregateId addressed => Copy(addressed.EventPropertyFilter),
        IProjectionId addressed => Copy(addressed.EventPropertyFilter),
        _ => null
    };

    /// <summary>
    /// Gets whether both halves were recovered, which is what the store needs before it can be asked
    /// to write: it folds a stream through an identifier, and one without the other addresses
    /// nothing.
    /// </summary>
    public bool IsRecovered => Stream is not null && Identifier is not null;

    /// <summary>
    /// Works out which stream and which identifier produced the ids one stored row is keyed by.
    /// </summary>
    /// <param name="catalogue">What the uploaded assemblies declare.</param>
    /// <param name="kind">Which of the two models the row holds.</param>
    /// <param name="model">The model type the row was folded into.</param>
    /// <param name="streamId">The stream it was folded from, as the store holds it.</param>
    /// <param name="addressedId">
    /// The id the identifier produced, which is the half of the store's key that is not the type
    /// version — or null when the row carries none to match.
    /// </param>
    public static StreamedIdentity Of(
        DomainTypeCatalogue catalogue,
        StreamedModelKind kind,
        Type model,
        string streamId,
        string? addressedId)
    {
        var streamType = Writes(catalogue.StreamedStreamIds, streamId);

        // Only the identifiers that address this model, because two models may be named by ids of
        // one shape and only one of them is the row's.
        var identifierType = addressedId is null
            ? null
            : Writes(DomainTypeDescriber.IdentifiersOf(model, catalogue.Identifiers(kind)), addressedId);

        // Asked once and used twice: the values are what the identifier is built from, so reading
        // them and building it are the same question answered to two different depths.
        var shape = identifierType is null ? null : IdShape.Of(identifierType);

        return new StreamedIdentity(
            streamType,
            streamType is null ? null : IdShape.Of(streamType)?.Rebuild(streamId),
            identifierType,
            shape?.Rebuild(addressedId!),
            shape?.ValuesFrom(addressedId!));
    }

    /// <summary>Which of these types writes ids of one stored id's shape, if any does.</summary>
    /// <remarks>
    /// Two that both could would leave it unanswered by whichever was scanned first, which is a
    /// guess. Taking the first is the same answer the lists take, and the page shows the id either
    /// way.
    /// </remarks>
    private static Type? Writes(IReadOnlyList<Type> candidates, string id) =>
        candidates.FirstOrDefault(candidate => IdShape.Of(candidate)?.Matches(id) == true);

    /// <summary>
    /// The claim as this record hands it on: the identifier's own dictionary is its to change, and
    /// an identifier claiming nothing folds the whole stream rather than leaving the claim unknown.
    /// </summary>
    private static IReadOnlyDictionary<string, string> Copy(IDictionary<string, string>? filter) =>
        filter?.ToDictionary(property => property.Key, property => property.Value) ?? [];
}
