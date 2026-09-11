using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// The pattern the ids of one type all match.
/// </summary>
/// <remarks>
/// Worked out by building one with values chosen to be recognisable and reading the id it produces:
/// <c>CustomerStreamId("probe-0")</c> answers <c>customer:probe-0</c>, so every id that type makes
/// is <c>customer:</c> and then something. Nothing declares this — a stream and an identifier each
/// carry no attribute, and what the store keeps is the id, not the type that made it — so asking the
/// type is the only way to know it for one uploaded after the fact.
/// <para>
/// Streams and the identifiers that name models inside them are read the same way because they pose
/// the same question. What tells them apart is which column the answer is held against: a stream's
/// pattern narrows the stream column, an aggregate's or a projection's narrows the id column.
/// </para>
/// <para>
/// The companion to <see cref="IdentifierShape"/>, which probes a DCB identifier the same way for
/// the boundary it resolves to. Both stand their values in with <see cref="ProbeValues"/>, so the
/// two agree about which types can be probed at all.
/// </para>
/// </remarks>
public sealed class IdShape
{
    private IdShape(
        Type named, string pattern, IReadOnlyList<string> filter, IReadOnlyList<string>? slots)
    {
        Named = named;
        Pattern = pattern;
        Filter = filter;
        _slots = slots;
    }

    /// <summary>
    /// Which value fills each hole in the pattern, by the constructor parameter that supplied it,
    /// left to right — or null when the holes and the values cannot be lined up.
    /// </summary>
    /// <remarks>
    /// Not the constructor's own order: a type is free to write its values into an id in any order,
    /// and what a hole gives back is whatever was written into that position.
    /// </remarks>
    private readonly IReadOnlyList<string>? _slots;

    /// <summary>Gets the type this describes: a stream, or an identifier of a model in one.</summary>
    public Type Named { get; }

    /// <summary>
    /// Gets the pattern its ids match, with a wildcard where each value goes.
    /// </summary>
    /// <remarks>
    /// A LIKE pattern, because that is what it is for: narrowing the store to the rows one type
    /// wrote. Taken from the type's own id with the probe values swapped for wildcards rather than
    /// assembled from a prefix — however the id is really put together, the pattern is that same id,
    /// so it matches what was stored.
    /// <para>
    /// A <c>%</c> or <c>_</c> the type writes into its id itself stays a wildcard here, because the
    /// store's LIKE runs with no escape character. Such an id would match a little more widely than
    /// it should — the same way a DCB boundary pattern does, and for the same reason.
    /// </para>
    /// </remarks>
    public string Pattern { get; }

    /// <summary>
    /// Gets the properties an event has to carry to be this model's, by name, or nothing when the
    /// whole stream is folded.
    /// </summary>
    /// <remarks>
    /// What lets several models share a stream: the stream says which events to read, and this says
    /// which of them are one model's. Read off the same probe the pattern is, and only the keys are
    /// kept — the values beside them are the instance's, the way a DCB identifier's tag values are.
    /// <para>
    /// Empty for a stream, which narrows nothing: it is where events are put, and which of them are
    /// a model's is the identifier's question rather than its own.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Filter { get; }

    /// <summary>
    /// Whether one stored id is of this shape.
    /// </summary>
    /// <param name="id">The id as the store holds it.</param>
    /// <remarks>
    /// The same question the database is asked when it narrows a table, answered here for a row
    /// already read — which is what naming a row's identifier needs, since the store keeps the id
    /// and not the type that produced it. Written as the LIKE it is rather than as a prefix test:
    /// a value in the middle of an id leaves a hole in the middle of the pattern.
    /// <para>
    /// The wildcards belong to the pattern alone. Anything in the id itself is an ordinary
    /// character, which is what escaping the id before the pattern's own holes are punched in
    /// achieves.
    /// </para>
    /// </remarks>
    public bool Matches(string id) => Expression.IsMatch(id);

    /// <summary>
    /// Turns one stored id back into the values the type was built from.
    /// </summary>
    /// <param name="id">The id as the store holds it.</param>
    /// <returns>
    /// The values by parameter name, or null when this id is not of this shape or its holes cannot
    /// be lined up with the values that filled them.
    /// </returns>
    /// <remarks>
    /// The pattern read rather than written: probing puts values in and keeps what came out, and
    /// this takes an id that came out and says what must have gone in — which is how a page holding
    /// nothing but a stored id builds the stream or the identifier that wrote it.
    /// <para>
    /// A type taking no values answers with none: there is nothing to recover, and it is still
    /// built from nothing.
    /// </para>
    /// <para>
    /// Holes are read as narrowly as they can be, so an id written with a separator between two
    /// values splits where the separator is. Nothing separating two holes would leave the split
    /// between them arbitrary; the values come back whole rather than the read failing, because a
    /// type writing two values into an id with nothing between them has already made them
    /// indistinguishable in the store.
    /// </para>
    /// The DCB counterpart of <see cref="IdentifierShape.ValuesFrom"/>, which recovers the same
    /// values from the tags a boundary was stored under.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? ValuesFrom(string id)
    {
        if (_slots is null)
        {
            return null;
        }

        var match = Reader.Match(id);
        if (!match.Success)
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var slot = 0; slot < _slots.Count; slot++)
        {
            // The groups follow the holes, so the first group is the first hole and the name beside
            // it is the parameter that wrote there.
            values[_slots[slot]] = match.Groups[slot + 1].Value;
        }

        return values;
    }

    /// <summary>
    /// The pattern as something that can be held against a string, built once per shape because a
    /// page asks it of every row it draws.
    /// </summary>
    private Regex Expression => _expression ??= Compile(Pattern);

    private Regex? _expression;

    /// <summary>
    /// The same pattern with its holes remembered rather than discarded, for reading an id back
    /// into the values that made it. Built once, for the same reason.
    /// </summary>
    private Regex Reader => _reader ??= CompileReader(Pattern);

    private Regex? _reader;

    private static Regex Compile(string pattern)
    {
        // Escaped whole, then the two wildcards put back as what they mean. Escaping first is what
        // keeps a regular expression's own punctuation — a dot, a plus, a bracket — an ordinary
        // character of the id, as it is to the database.
        var expression = Regex.Escape(pattern)
            .Replace("%", ".*", StringComparison.Ordinal)
            .Replace("_", ".", StringComparison.Ordinal);

        return new Regex($"^{expression}$", RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// The same expression with a group around each hole, so what matched one can be read back out.
    /// </summary>
    /// <remarks>
    /// Every hole but the last is read as narrowly as it can be, and the last takes whatever is
    /// left: an id ending in a value should give the whole of it back, separators and all, while
    /// one with something written after a value stops at it.
    /// </remarks>
    private static Regex CompileReader(string pattern)
    {
        var escaped = Regex.Escape(pattern).Replace("_", ".", StringComparison.Ordinal);

        var parts = escaped.Split('%');

        var expression = string.Join(string.Empty, parts.Select((part, index) =>
            index == 0 ? part : $"({(index == parts.Length - 1 ? ".*" : ".*?")}){part}"));

        return new Regex($"^{expression}$", RegexOptions.CultureInvariant);
    }

    private static readonly ConcurrentDictionary<Type, IdShape?> Known = new();

    /// <summary>
    /// Works out the shape of a stream or an identifier.
    /// </summary>
    /// <param name="named">The type that produces the ids.</param>
    /// <returns>
    /// The shape, or null when it cannot be worked out — the type names nothing in a store, it
    /// cannot be built, or one of its values is of a kind nothing recognisable can be passed for,
    /// which would leave no way to tell which part of the id came from it.
    /// </returns>
    /// <remarks>
    /// Kept once worked out, including the finding that there is no shape: probing means building
    /// one and reading what it produces, and neither the type nor its answer changes while it is
    /// loaded. <see cref="Forget"/> clears it when the types are reloaded.
    /// </remarks>
    public static IdShape? Of(Type named) => Known.GetOrAdd(named, Probe);

    /// <summary>
    /// Drops what has been worked out, for when the uploaded assemblies are read again.
    /// </summary>
    public static void Forget() => Known.Clear();

    private static IdShape? Probe(Type named)
    {
        if (!Names(named))
        {
            return null;
        }

        var parameters = IdentifierFactory.Parameters(named);

        var probes = parameters
            .Select((parameter, index) => (parameter.Name, Probe: ProbeValues.For(parameter.TypeName, index)))
            .ToList();

        if (probes.Any(probe => probe.Probe is null))
        {
            return null;
        }

        // A type taking nothing names one thing and is built without values. Its id is fixed, so
        // there is no hole to leave in it — the pattern is that id, and matches the one row set it
        // was ever going to.
        var built = parameters.Count == 0
            ? Create(named)
            : IdentifierFactory.Create(named,
                probes.ToDictionary(probe => probe.Name, probe => (string?)probe.Probe)).Instance;

        if (IdOf(built) is not { } id || string.IsNullOrEmpty(id))
        {
            return null;
        }

        var pattern = probes.Aggregate(id,
            (rendered, probe) => rendered.Replace(probe.Probe!, "%", StringComparison.Ordinal));

        return new IdShape(named, pattern, FilterOf(built), Slots(id, probes));
    }

    /// <summary>
    /// Which parameter wrote into each hole, left to right along the id.
    /// </summary>
    /// <param name="id">The id the probe values produced.</param>
    /// <param name="probes">The value stood in for each parameter, in constructor order.</param>
    /// <returns>
    /// The parameter names in the order their values appear, or null when a value reached no hole
    /// or reached more than one — either way the holes and the values cannot be lined up, so an id
    /// of this shape cannot be read back into the values that made it.
    /// </returns>
    /// <remarks>
    /// A type taking nothing has no holes and no values, which lines up trivially: there is nothing
    /// to recover and nothing needed to build one.
    /// </remarks>
    private static IReadOnlyList<string>? Slots(
        string id, IReadOnlyList<(string Name, string? Probe)> probes)
    {
        var placed = new List<(int At, string Name)>();

        foreach (var (name, probe) in probes)
        {
            var at = id.IndexOf(probe!, StringComparison.Ordinal);

            // Written nowhere, or written twice: in the first case the value is lost, in the second
            // the pattern has more holes than values and neither hole is that value's alone.
            if (at < 0 || id.IndexOf(probe!, at + probe!.Length, StringComparison.Ordinal) >= 0)
            {
                return null;
            }

            placed.Add((at, name));
        }

        return [.. placed.OrderBy(slot => slot.At).Select(slot => slot.Name)];
    }

    /// <summary>
    /// The properties one built identifier narrows its stream by, in the order it names them.
    /// </summary>
    /// <remarks>
    /// Reading it is the type's own code, like the id beside it. One that throws narrows by nothing
    /// as far as this can tell, which is what an identifier with no filter says too — the page it
    /// reaches is about the type, not about what could not be asked of it.
    /// </remarks>
    private static IReadOnlyList<string> FilterOf(object? built)
    {
        try
        {
            return built switch
            {
                IAggregateId aggregateId => Keys(aggregateId.EventPropertyFilter),
                IProjectionId projectionId => Keys(projectionId.EventPropertyFilter),
                _ => []
            };
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> Keys(IDictionary<string, string>? filter) =>
        filter is null ? [] : [.. filter.Keys];

    /// <summary>
    /// Whether a type names something the store keeps by id: a stream, or a model inside one.
    /// </summary>
    /// <remarks>
    /// The DCB identifiers are deliberately not here. They name a boundary rather than an id, and
    /// what is stored of one is its tags — a question <see cref="IdentifierShape"/> answers.
    /// </remarks>
    private static bool Names(Type named) =>
        typeof(IStreamId).IsAssignableFrom(named) ||
        typeof(IAggregateId).IsAssignableFrom(named) ||
        typeof(IProjectionId).IsAssignableFrom(named);

    /// <summary>
    /// The id one built instance produces, or null when it produces none.
    /// </summary>
    /// <remarks>
    /// Reading it is the type's own code. One that throws costs its own row in a dropdown, not the
    /// page.
    /// </remarks>
    private static string? IdOf(object? built)
    {
        try
        {
            return built switch
            {
                IStreamId stream => stream.Id,
                IAggregateId aggregateId => aggregateId.Id,
                IProjectionId projectionId => projectionId.Id,
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a type that takes nothing. <see cref="IdentifierFactory"/> is no use here: it reads
    /// the constructor asking for the most values and there is none to find.
    /// </summary>
    private static object? Create(Type named)
    {
        try
        {
            return Activator.CreateInstance(named);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
