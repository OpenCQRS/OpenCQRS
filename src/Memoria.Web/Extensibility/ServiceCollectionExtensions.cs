using System.Reflection;
using Memoria.EventSourcing.Dcb.Extensions;
using Memoria.EventSourcing.Extensions;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Registration for the domain types uploaded through the settings page.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Loads the uploaded assemblies, binds the domain types in them to both event sourcing
    /// models, and registers the catalogue the pages list.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="store">Where the uploaded assemblies live.</param>
    /// <returns>What was found, so start-up can log it.</returns>
    /// <remarks>
    /// Call before <c>AddMemoriaEntityFrameworkCore</c> and <c>AddMemoriaDcbEntityFrameworkCore</c>.
    /// Binding an assembly re-registers each model's no-op domain service, and a store call
    /// replaces that service — so a bind that ran afterwards would put the no-op back.
    /// <para>
    /// Every failure is collected onto the catalogue rather than thrown. An upload that cannot be
    /// bound — two assemblies claiming one event name and version, most likely — must not stop the
    /// application, because the settings page is the only way to replace it.
    /// </para>
    /// </remarks>
    public static DomainTypeCatalogue AddDomainExtensions(this IServiceCollection services, ExtensionStore store)
    {
        var loaded = ExtensionLoader.Load(store);
        var scanned = DomainTypeScanner.Scan(loaded.Assemblies);
        var errors = new List<string>([.. loaded.Errors, .. scanned.Errors]);

        foreach (var assembly in loaded.Assemblies)
        {
            var marker = scanned.Events.Concat(scanned.StreamedAggregates).Concat(scanned.DcbAggregates)
                .FirstOrDefault(type => type.Assembly == assembly);

            if (marker is null)
            {
                continue;
            }

            // One assembly at a time, so a name already claimed by another upload costs that
            // assembly's bindings rather than every assembly's.
            Bind(() => services.AddMemoriaEventSourcing(marker), assembly, "streamed", errors);
            Bind(() => services.AddMemoriaDcb(marker), assembly, "DCB", errors);
        }

        var catalogue = scanned with { Errors = errors };

        services.AddSingleton(store);
        services.AddSingleton(catalogue);

        return catalogue;
    }

    private static void Bind(Action bind, Assembly assembly, string model, List<string> errors)
    {
        try
        {
            bind();
        }
        catch (Exception exception)
        {
            errors.Add($"{assembly.GetName().Name} ({model}): {exception.Message}");
        }
    }
}
