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
    /// Read into memory and loaded from the bytes rather than from the path, for two reasons that
    /// both come from reloading without restarting. Loading from a path holds the file open, which
    /// would stop the next upload writing over it; and it caches by path, so a replaced assembly
    /// would go on being served from the copy loaded the first time round.
    /// <para>
    /// A file that is not a managed assembly, or one whose dependencies are missing, is reported
    /// rather than thrown: a bad upload must leave the application running and able to say so.
    /// </para>
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
                // Into the default context, not a private one: an uploaded assembly references
                // Memoria's own assemblies, and those types have to be the ones this process
                // already loaded, or nothing it holds would satisfy IEvent or IAggregateRoot.
                assemblies.Add(Assembly.Load(File.ReadAllBytes(path)));
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
                return File.Exists(candidate) ? Assembly.Load(File.ReadAllBytes(candidate)) : null;
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
