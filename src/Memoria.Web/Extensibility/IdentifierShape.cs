using Memoria.EventSourcing.Dcb;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Which tag each of an identifier's values ends up in.
/// </summary>
/// <remarks>
/// Worked out by building one with values chosen to be recognisable and reading the boundary it
/// produces: a tag carrying the value given for <c>productId</c> tells us that parameter fills the
/// <c>product</c> tag. Nothing declares this, and asking the identifier is the only way to know it
/// for a type uploaded after the fact.
/// </remarks>
public sealed class IdentifierShape
{
    private IdentifierShape(Type identifier, IReadOnlyList<TagSlot> slots)
    {
        Identifier = identifier;
        Slots = slots;
    }

    /// <summary>Gets the identifier type this describes.</summary>
    public Type Identifier { get; }

    /// <summary>Gets one slot per value, in constructor order.</summary>
    public IReadOnlyList<TagSlot> Slots { get; }

    /// <summary>
    /// Works out the shape of an identifier.
    /// </summary>
    /// <param name="identifier">The identifier type.</param>
    /// <returns>
    /// The shape, or null when it cannot be worked out — the type is not a DCB identifier, it
    /// cannot be built from values, or one of its values reaches no tag and so could never be
    /// recovered from the store.
    /// </returns>
    public static IdentifierShape? Of(Type identifier)
    {
        if (!typeof(IDcbAggregateId).IsAssignableFrom(identifier) &&
            !typeof(IDcbProjectionId).IsAssignableFrom(identifier))
        {
            return null;
        }

        var parameters = IdentifierFactory.Parameters(identifier);
        if (parameters.Count == 0)
        {
            return null;
        }

        var probes = parameters
            .Select((parameter, index) => (parameter.Name, Probe: Probes.For(parameter.TypeName, index)))
            .ToList();

        if (probes.Any(probe => probe.Probe is null))
        {
            return null;
        }

        var created = IdentifierFactory.Create(identifier,
            probes.ToDictionary(probe => probe.Name, probe => (string?)probe.Probe));

        var boundary = created.Instance switch
        {
            IDcbAggregateId aggregateId => aggregateId.Boundary,
            IDcbProjectionId projectionId => projectionId.Boundary,
            _ => null
        };

        if (boundary is null)
        {
            return null;
        }

        var slots = new List<TagSlot>();

        foreach (var (name, probe) in probes)
        {
            var tag = boundary.Tags.FirstOrDefault(candidate => candidate.Value == probe);

            // A value that reaches no tag cannot be read back out of the store, so the whole shape
            // is unusable rather than partly usable.
            if (tag.Key is null)
            {
                return null;
            }

            slots.Add(new TagSlot(tag.Key, name));
        }

        return new IdentifierShape(identifier, slots);
    }

    /// <summary>
    /// Turns the tags of one stored instance back into the values its identifier was built from.
    /// </summary>
    /// <param name="tags">The tags carried by that instance, as <c>key:value</c>.</param>
    /// <returns>The values by parameter name, or null when a tag the shape needs is not there.</returns>
    public IReadOnlyDictionary<string, string>? ValuesFrom(IEnumerable<string> tags)
    {
        var byKey = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            var parsed = Tag.Parse(tag);
            byKey.TryAdd(parsed.Key, parsed.Value);
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var slot in Slots)
        {
            if (!byKey.TryGetValue(slot.TagKey, out var value))
            {
                return null;
            }

            values[slot.ParameterName] = value;
        }

        return values;
    }

    /// <summary>The tag keys an instance has to carry to be one of these.</summary>
    public IReadOnlyList<string> TagKeys => Slots.Select(slot => slot.TagKey).ToList();

    /// <summary>
    /// Values distinctive enough to be spotted again in the tags they end up in.
    /// </summary>
    private static class Probes
    {
        public static string? For(string typeName, int index) => typeName switch
        {
            "string" => $"probe-{index}",
            "Guid" => Guid.NewGuid().ToString(),
            "int" or "long" or "short" => (900_000_000 + index).ToString(),
            _ => null
        };
    }
}

/// <summary>One value of an identifier, and the tag it is written into.</summary>
/// <param name="TagKey">The tag's key.</param>
/// <param name="ParameterName">The constructor parameter supplying its value.</param>
public sealed record TagSlot(string TagKey, string ParameterName);
