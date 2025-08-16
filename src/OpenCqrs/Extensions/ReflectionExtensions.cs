using System.Reflection;

namespace OpenCqrs.Extensions;

public static class ReflectionExtensions
{
    public static IEnumerable<T> GetImplementationsOf<T>(this Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(type => type.GetTypeInfo().IsClass && !type.GetTypeInfo().IsAbstract && typeof(T).IsAssignableFrom(type))
            .ToList();

        return types.Select(type => (T)Activator.CreateInstance(type)!).ToList();
    }

    public static IEnumerable<T> GetImplementationsOf<T>(this IEnumerable<Assembly> assemblies)
    {
        var result = new List<T>();

        foreach (var assembly in assemblies)
        {
            result.AddRange(assembly.GetImplementationsOf<T>());
        }

        return result;
    }
}
