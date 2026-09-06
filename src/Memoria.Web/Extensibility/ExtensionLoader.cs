using System.Reflection;
using System.Runtime.Loader;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Loads the assemblies taken out of the uploaded archives.
/// </summary>
public static class ExtensionLoader
{
    private static bool _resolverInstalled;
    private static readonly Lock ResolverGate = new();

    /// <summary>
    /// Loads every assembly in the store's library directory.
    /// </summary>
    /// <param name="store">Where the assemblies were extracted to.</param>
    /// <returns>What loaded, and a line for each file that did not.</returns>
    /// <remarks>
    /// A file that is not a managed assembly, or one whose dependencies are missing, is reported
    /// rather than thrown: this runs during start-up, and a bad upload must still leave an
    /// application running that can be used to replace it.
    /// </remarks>
    public static LoadedExtensions Load(ExtensionStore store)
    {
        var assemblies = new List<Assembly>();
        var errors = new List<string>();

        var paths = store.AssemblyPaths();
        if (paths.Count == 0)
        {
            return new LoadedExtensions([], []);
        }

        InstallDependencyResolver(store.LibraryDirectory);

        foreach (var path in paths)
        {
            try
            {
                // LoadFrom rather than a private context: an uploaded assembly references Memoria's
                // own assemblies, and those types must be the ones this process already loaded, or
                // nothing it contains would satisfy IEvent or IAggregateRoot.
                assemblies.Add(Assembly.LoadFrom(path));
            }
            catch (Exception exception)
            {
                errors.Add($"{Path.GetFileName(path)}: {exception.Message}");
            }
        }

        return new LoadedExtensions(assemblies, errors);
    }

    /// <summary>
    /// Lets an uploaded assembly find its own dependencies in the library directory, for the ones
    /// the application does not already carry.
    /// </summary>
    private static void InstallDependencyResolver(string libraryDirectory)
    {
        lock (ResolverGate)
        {
            if (_resolverInstalled)
            {
                return;
            }

            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var candidate = Path.Combine(libraryDirectory, $"{name.Name}.dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };

            _resolverInstalled = true;
        }
    }
}

/// <summary>
/// The outcome of a load.
/// </summary>
/// <param name="Assemblies">The assemblies that loaded.</param>
/// <param name="Errors">One line per file that did not.</param>
public sealed record LoadedExtensions(IReadOnlyList<Assembly> Assemblies, IReadOnlyList<string> Errors);
