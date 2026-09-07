using System.Collections.Concurrent;
using System.Text;
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
    private IdentifierShape(Type identifier, IReadOnlyList<TagSlot> slots, string boundaryPattern)
    {
        Identifier = identifier;
        Slots = slots;
        BoundaryPattern = boundaryPattern;
    }

    /// <summary>Gets the identifier type this describes.</summary>
    public Type Identifier { get; }

    /// <summary>Gets one slot per value, in constructor order.</summary>
    public IReadOnlyList<TagSlot> Slots { get; }

    /// <summary>
    /// Gets the pattern a boundary of this shape matches, with a wildcard where each value goes.
    /// </summary>
    /// <remarks>
    /// Taken from the identifier's own canonical rendering with the probe values swapped for
    /// wildcards, rather than assembled from the tag keys. However the boundary is really put
    /// together — which tags are grouped, how they are ordered, what separates them — the pattern
    /// is that same rendering, so it matches what was stored.
    /// </remarks>
    public string BoundaryPattern { get; }

    private static readonly ConcurrentDictionary<Type, IdentifierShape?> Known = new();

    /// <summary>
    /// Works out the shape of an identifier.
    /// </summary>
    /// <param name="identifier">The identifier type.</param>
    /// <returns>
    /// The shape, or null when it cannot be worked out — the type is not a DCB identifier, it
    /// cannot be built from values, or one of its values reaches no tag and so could never be
    /// recovered from the store.
    /// </returns>
    /// <remarks>
    /// Kept once worked out, including the finding that there is no shape: probing means building
    /// an identifier and reading the boundary it produces, and neither the type nor its answer
    /// changes while it is loaded. <see cref="Forget"/> clears it when the types are reloaded.
    /// </remarks>
    public static IdentifierShape? Of(Type identifier) => Known.GetOrAdd(identifier, Probe);

    /// <summary>
    /// Drops what has been worked out, for when the uploaded assemblies are read again.
    /// </summary>
    public static void Forget() => Known.Clear();

    private static IdentifierShape? Probe(Type identifier)
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

        var pattern = probes.Aggregate(boundary.ToString(),
            (rendered, probe) => rendered.Replace(probe.Probe!, "%", StringComparison.Ordinal));

        return new IdentifierShape(identifier, slots, pattern);
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
    /// Turns a stored boundary back into the values its identifier was built from.
    /// </summary>
    /// <param name="boundary">The boundary in canonical form, as the store holds it.</param>
    /// <returns>The values by parameter name, or null when a tag the shape needs is not there.</returns>
    /// <remarks>
    /// Tags are separated by a comma between groups and an ampersand inside one, either of which a
    /// tag may itself contain escaped with a backslash — so the split has to respect the escape
    /// rather than call <c>Split</c>.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? ValuesFromBoundary(string boundary) =>
        ValuesFrom(SplitTags(boundary));

    private static IEnumerable<string> SplitTags(string boundary)
    {
        var tag = new StringBuilder();

        for (var index = 0; index < boundary.Length; index++)
        {
            var character = boundary[index];

            if (character == '\\' && index + 1 < boundary.Length)
            {
                tag.Append(boundary[++index]);
                continue;
            }

            if (character is ',' or '&')
            {
                yield return tag.ToString();
                tag.Clear();
                continue;
            }

            tag.Append(character);
        }

        yield return tag.ToString();
    }

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
