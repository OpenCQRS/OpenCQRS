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
    private IdShape(Type named, string pattern, IReadOnlyList<string> filter)
    {
        Named = named;
        Pattern = pattern;
        Filter = filter;
    }

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
    /// The pattern as something that can be held against a string, built once per shape because a
    /// page asks it of every row it draws.
    /// </summary>
    private Regex Expression => _expression ??= Compile(Pattern);

    private Regex? _expression;

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

        return new IdShape(named, pattern, FilterOf(built));
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
