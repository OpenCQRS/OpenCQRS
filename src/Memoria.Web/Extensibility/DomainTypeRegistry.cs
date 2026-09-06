using System.Reflection;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Holds the domain types the application knows about, and rebuilds them from scratch on demand.
/// </summary>
/// <param name="store">Where the uploaded assemblies live.</param>
/// <param name="host">
/// The application's own assembly, scanned alongside the uploads so anything it declares survives a
/// reload. Null when there is nothing of its own to contribute.
/// </param>
/// <remarks>
/// Reloading replaces process-wide state: Memoria keeps its binding maps in static properties on
/// <see cref="TypeBindings"/> and <see cref="DcbTypeBindings"/>, so a reload changes what every
/// request on this server resolves, for everyone, at once. That is what makes an upload take effect
/// without a restart, and it is also why one bad upload is everybody's problem. Pages already
/// rendered are not told; a browser refresh picks the new set up.
/// </remarks>
public sealed class DomainTypeRegistry(ExtensionStore store, Assembly? host = null)
{
    private readonly Lock _gate = new();

    /// <summary>Gets the types found by the last reload.</summary>
    public DomainTypeCatalogue Current { get; private set; } = DomainTypeCatalogue.Empty;

    /// <summary>
    /// Reads every uploaded assembly again and rebuilds the bindings from nothing.
    /// </summary>
    /// <remarks>
    /// From nothing rather than merged into what is already there, so a type that has been removed
    /// or renamed stops being offered. Each map is built complete and then assigned in one go: a
    /// request reading a map concurrently sees either the whole previous set or the whole new one,
    /// never half of each.
    /// </remarks>
    public void Reload()
    {
        lock (_gate)
        {
            var loaded = ExtensionLoader.Load(store);

            var assemblies = host is null
                ? loaded.Assemblies
                : new List<Assembly>([host, .. loaded.Assemblies]);

            var scanned = DomainTypeScanner.Scan(assemblies);
            var errors = new List<string>([.. loaded.Errors, .. scanned.Errors]);

            var events = Bind(scanned.Events, KeyOf<EventType>, "event", errors);
            var aggregates = Bind(scanned.StreamedAggregates, KeyOf<AggregateType>, "aggregate", errors);
            var projections = Bind(scanned.StreamedProjections, KeyOf<ProjectionType>, "projection", errors);
            var dcbAggregates = Bind(scanned.DcbAggregates, KeyOf<AggregateType>, "DCB aggregate", errors);
            var dcbProjections = Bind(scanned.DcbProjections, KeyOf<ProjectionType>, "DCB projection", errors);

            TypeBindings.EventTypeBindings = events;
            TypeBindings.AggregateTypeBindings = aggregates;
            TypeBindings.ProjectionTypeBindings = projections;
            DcbTypeBindings.AggregateTypeBindings = dcbAggregates;
            DcbTypeBindings.ProjectionTypeBindings = dcbProjections;

            Current = scanned with { Errors = errors, ReloadedUtc = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// Builds one binding map. A name and version claimed by two types is reported and the first
    /// keeps the name, rather than the reload failing and leaving nothing bound at all.
    /// </summary>
    private static Dictionary<string, Type> Bind(
        IEnumerable<Type> types, Func<Type, string?> keyOf, string what, List<string> errors)
    {
        var bindings = new Dictionary<string, Type>();

        foreach (var type in types)
        {
            // No attribute means the type is not meant to be bound; only an attributed type is.
            var key = keyOf(type);
            if (key is null)
            {
                continue;
            }

            if (bindings.TryGetValue(key, out var claimed))
            {
                errors.Add($"Two types claim the {what} name {key}: " +
                           $"{claimed.FullName} and {type.FullName}. {claimed.Name} keeps it.");
                continue;
            }

            bindings.Add(key, type);
        }

        return bindings;
    }

    private static string? KeyOf<TAttribute>(Type type) where TAttribute : Attribute =>
        type.GetCustomAttribute<TAttribute>() switch
        {
            EventType attribute => TypeBindings.GetTypeBindingKey(attribute.Name, attribute.Version),
            AggregateType attribute => TypeBindings.GetTypeBindingKey(attribute.Name, attribute.Version),
            ProjectionType attribute => TypeBindings.GetTypeBindingKey(attribute.Name, attribute.Version),
            _ => null
        };
}
