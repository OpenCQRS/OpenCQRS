using System.Reflection;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads the domain types out of assemblies uploaded through the settings page.
/// </summary>
public static class DomainTypeScanner
{
    /// <summary>
    /// Scans the supplied assemblies for both models' aggregates, projections, their identifiers,
    /// the streams the streamed model is folded from, and events.
    /// </summary>
    /// <param name="assemblies">The assemblies to read.</param>
    /// <returns>What was found, plus a line for every assembly that could not be read.</returns>
    public static DomainTypeCatalogue Scan(IReadOnlyList<Assembly> assemblies)
    {
        var types = new List<Type>();
        var errors = new List<string>();

        foreach (var assembly in assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException exception)
            {
                // Some types loaded and some did not. The ones that did are still worth listing.
                types.AddRange(exception.Types.OfType<Type>());
                errors.Add($"{assembly.GetName().Name}: {Describe(exception)}");
            }
            catch (Exception exception)
            {
                errors.Add($"{assembly.GetName().Name}: {exception.Message}");
            }
        }

        var concrete = types
            .Where(type => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition)
            .Distinct()
            .ToList();

        // A DCB model is not a streamed one and never both, so each list excludes the other's
        // marker rather than relying on the class hierarchies staying disjoint.
        var streamedAggregates = Implementing<IAggregateRoot>(concrete);
        var streamedProjections = Implementing<IProjection>(concrete);
        var dcbAggregates = Implementing<IDcbAggregateRoot>(concrete);
        var dcbProjections = Implementing<IDcbProjection>(concrete);
        var events = Implementing<IEvent>(concrete);

        return new DomainTypeCatalogue
        {
            StreamedStreamIds = Implementing<IStreamId>(concrete),
            StreamedAggregates = streamedAggregates,
            StreamedAggregateIds = Implementing<IAggregateId>(concrete),
            StreamedProjections = streamedProjections,
            StreamedProjectionIds = Implementing<IProjectionId>(concrete),
            DcbAggregates = dcbAggregates,
            DcbAggregateIds = Implementing<IDcbAggregateId>(concrete),
            DcbProjections = dcbProjections,
            DcbProjectionIds = Implementing<IDcbProjectionId>(concrete),
            Events = events,
            // Worked out here rather than by each page that wants it, because reading a filter means
            // building a model and the answer only changes when these assemblies are read again.
            StreamedEvents = AppliedBy([..streamedAggregates, ..streamedProjections], events),
            DcbEvents = AppliedBy([..dcbAggregates, ..dcbProjections], events),
            Errors = errors
        };
    }

    /// <summary>
    /// The events some model of one consistency model applies, out of the events that were found.
    /// </summary>
    /// <param name="models">The aggregates and projections of that consistency model.</param>
    /// <param name="events">Every event found, which is what the answer is drawn from.</param>
    /// <returns>
    /// Those events, in the order they were found in. A model applying an event that was not found
    /// adds nothing: it cannot be listed, so it is not.
    /// </returns>
    /// <remarks>
    /// The union across the models, because one model applying an event is enough for the model as
    /// a whole to. A model declaring no filter applies whatever its stream or boundary hands it, so
    /// it applies all of them — one such model is enough to make the answer the whole set, which is
    /// the truthful answer rather than a useful-looking one.
    /// </remarks>
    public static IReadOnlyList<Type> AppliedBy(
        IReadOnlyList<Type> models, IReadOnlyList<Type> events)
    {
        var applied = new HashSet<Type>();

        foreach (var model in models)
        {
            if (BoundaryEvents.AppliedBy(model, null) is not { } filter)
            {
                return events;
            }

            applied.UnionWith(filter);
        }

        return events.Where(applied.Contains).ToList();
    }

    private static IReadOnlyList<Type> Implementing<T>(IEnumerable<Type> types) =>
        types.Where(type => typeof(T).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

    private static string Describe(ReflectionTypeLoadException exception)
    {
        var reasons = exception.LoaderExceptions
            .OfType<Exception>()
            .Select(loaderException => loaderException.Message)
            .Distinct()
            .Take(3);

        return string.Join("; ", reasons);
    }
}
