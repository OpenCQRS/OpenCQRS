using System.Collections.Concurrent;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// The pattern the ids of one stream type all match.
/// </summary>
/// <remarks>
/// Worked out by building one with values chosen to be recognisable and reading the id it produces:
/// <c>CustomerStreamId("probe-0")</c> answers <c>customer:probe-0</c>, so every id that type makes
/// is <c>customer:</c> and then something. Nothing declares this — a stream carries no attribute,
/// and what the log stores is the id, not the type that made it — so asking the type is the only
/// way to know it for one uploaded after the fact.
/// <para>
/// The companion to <see cref="IdentifierShape"/>, which probes a DCB identifier the same way for
/// the boundary it resolves to. Both stand their values in with <see cref="ProbeValues"/>, so the
/// two agree about which types can be probed at all.
/// </para>
/// </remarks>
public sealed class StreamShape
{
    private StreamShape(Type streamId, string pattern)
    {
        StreamId = streamId;
        Pattern = pattern;
    }

    /// <summary>Gets the stream type this describes.</summary>
    public Type StreamId { get; }

    /// <summary>
    /// Gets the pattern its ids match, with a wildcard where each value goes.
    /// </summary>
    /// <remarks>
    /// A LIKE pattern, because that is what it is for: narrowing the log to the rows one stream type
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

    private static readonly ConcurrentDictionary<Type, StreamShape?> Known = new();

    /// <summary>
    /// Works out the shape of a stream type.
    /// </summary>
    /// <param name="streamId">The stream type.</param>
    /// <returns>
    /// The shape, or null when it cannot be worked out — the type is not a stream, it cannot be
    /// built, or one of its values is of a kind nothing recognisable can be passed for, which would
    /// leave no way to tell which part of the id came from it.
    /// </returns>
    /// <remarks>
    /// Kept once worked out, including the finding that there is no shape: probing means building a
    /// stream and reading what it produces, and neither the type nor its answer changes while it is
    /// loaded. <see cref="Forget"/> clears it when the types are reloaded.
    /// </remarks>
    public static StreamShape? Of(Type streamId) => Known.GetOrAdd(streamId, Probe);

    /// <summary>
    /// Drops what has been worked out, for when the uploaded assemblies are read again.
    /// </summary>
    public static void Forget() => Known.Clear();

    private static StreamShape? Probe(Type streamId)
    {
        if (!typeof(IStreamId).IsAssignableFrom(streamId))
        {
            return null;
        }

        var parameters = IdentifierFactory.Parameters(streamId);

        var probes = parameters
            .Select((parameter, index) => (parameter.Name, Probe: ProbeValues.For(parameter.TypeName, index)))
            .ToList();

        if (probes.Any(probe => probe.Probe is null))
        {
            return null;
        }

        // A stream taking nothing names one stream and is built without values. Its id is fixed, so
        // there is no hole to leave in it — the pattern is that id, and matches the one row set it
        // was ever going to.
        var built = parameters.Count == 0
            ? Create(streamId)
            : IdentifierFactory.Create(streamId,
                probes.ToDictionary(probe => probe.Name, probe => (string?)probe.Probe)).Instance;

        if (built is not IStreamId stream)
        {
            return null;
        }

        string id;

        try
        {
            id = stream.Id;
        }
        catch (Exception)
        {
            // Reading the id is the type's own code. A stream that throws costs its own row in the
            // dropdown, not the page.
            return null;
        }

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var pattern = probes.Aggregate(id,
            (rendered, probe) => rendered.Replace(probe.Probe!, "%", StringComparison.Ordinal));

        return new StreamShape(streamId, pattern);
    }

    /// <summary>
    /// Builds a stream that takes nothing. <see cref="IdentifierFactory"/> is no use here: it reads
    /// the constructor asking for the most values and there is none to find.
    /// </summary>
    private static object? Create(Type streamId)
    {
        try
        {
            return Activator.CreateInstance(streamId);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
