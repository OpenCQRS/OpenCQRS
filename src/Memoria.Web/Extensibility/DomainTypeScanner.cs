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
    /// and events.
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

        return new DomainTypeCatalogue
        {
            // A DCB model is not a streamed one and never both, so each list excludes the other's
            // marker rather than relying on the class hierarchies staying disjoint.
            StreamedAggregates = Implementing<IAggregateRoot>(concrete),
            StreamedAggregateIds = Implementing<IAggregateId>(concrete),
            StreamedProjections = Implementing<IProjection>(concrete),
            StreamedProjectionIds = Implementing<IProjectionId>(concrete),
            DcbAggregates = Implementing<IDcbAggregateRoot>(concrete),
            DcbAggregateIds = Implementing<IDcbAggregateId>(concrete),
            DcbProjections = Implementing<IDcbProjection>(concrete),
            DcbProjectionIds = Implementing<IDcbProjectionId>(concrete),
            Events = Implementing<IEvent>(concrete),
            Errors = errors
        };
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
