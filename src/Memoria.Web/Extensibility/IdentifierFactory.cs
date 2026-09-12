using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Builds an aggregate or projection identifier out of values typed into a page.
/// </summary>
/// <remarks>
/// An identifier is the only way to ask the store for anything, and the values it needs are the
/// user's to supply — a product id, the sku that shares its boundary. What those values are is
/// read off the constructor, so an identifier the framework never saw before still works.
/// </remarks>
public static class IdentifierFactory
{
    /// <summary>
    /// The values an identifier has to be given, in constructor order.
    /// </summary>
    /// <param name="identifier">The identifier type.</param>
    /// <remarks>
    /// Read from the constructor taking the most parameters, which is the one that asks for the
    /// whole boundary — <c>ProductCreationId(productId, sku)</c> rather than a narrower overload.
    /// </remarks>
    public static IReadOnlyList<IdentifierParameter> Parameters(Type identifier) =>
        Constructor(identifier)?.GetParameters()
            .Select(parameter => new IdentifierParameter(
                parameter.Name ?? "?", DomainTypeDescriber.Readable(parameter.ParameterType)))
            .ToList() ?? [];

    /// <summary>
    /// Whether every value the identifier needs has been supplied.
    /// </summary>
    /// <param name="identifier">The identifier type.</param>
    /// <param name="values">The values so far, by parameter name.</param>
    public static bool IsComplete(Type identifier, IReadOnlyDictionary<string, string?> values)
    {
        var parameters = Parameters(identifier);

        return parameters.Count > 0 && parameters.All(parameter =>
            !string.IsNullOrWhiteSpace(Lookup(values, parameter.Name)));
    }

    /// <summary>
    /// Builds the identifier.
    /// </summary>
    /// <param name="identifier">The identifier type.</param>
    /// <param name="values">The values, by parameter name, as they were typed.</param>
    /// <returns>The identifier, or the reason it could not be built.</returns>
    public static CreatedIdentifier Create(Type identifier, IReadOnlyDictionary<string, string?> values)
    {
        var constructor = Constructor(identifier);

        if (constructor is null)
        {
            return new CreatedIdentifier(null, $"{identifier.Name} has no public constructor to call.");
        }

        var arguments = new List<object?>();

        foreach (var parameter in constructor.GetParameters())
        {
            var name = parameter.Name ?? "?";
            var value = Lookup(values, name);

            if (string.IsNullOrWhiteSpace(value))
            {
                return new CreatedIdentifier(null, $"A value for {name} is needed.");
            }

            try
            {
                arguments.Add(Convert(value, parameter.ParameterType));
            }
            catch (Exception exception)
            {
                return new CreatedIdentifier(null,
                    $"{name} is not a valid {DomainTypeDescriber.Readable(parameter.ParameterType)}: " +
                    exception.Message);
            }
        }

        try
        {
            return new CreatedIdentifier(constructor.Invoke([.. arguments]), null);
        }
        catch (TargetInvocationException exception)
        {
            // The identifier's own constructor rejected the values — its rule, so its wording.
            return new CreatedIdentifier(null, exception.InnerException?.Message ?? exception.Message);
        }
        catch (Exception exception)
        {
            return new CreatedIdentifier(null, exception.Message);
        }
    }

    /// <summary>
    /// Finds a value by parameter name, ignoring case: the names arrive on a query string, where
    /// matching the constructor's casing is nobody's job.
    /// </summary>
    private static string? Lookup(IReadOnlyDictionary<string, string?> values, string name) =>
        values.FirstOrDefault(value =>
            string.Equals(value.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static ConstructorInfo? Constructor(Type identifier) =>
        identifier.GetConstructors()
            .Where(constructor => constructor.GetParameters().Length > 0)
            .MaxBy(constructor => constructor.GetParameters().Length);

    private static object? Convert(string value, Type wanted)
    {
        var target = Nullable.GetUnderlyingType(wanted) ?? wanted;

        if (target == typeof(string))
        {
            return value;
        }

        var converter = TypeDescriptor.GetConverter(target);

        return converter.CanConvertFrom(typeof(string))
            ? converter.ConvertFromString(null, CultureInfo.InvariantCulture, value)
            : System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }
}

/// <summary>One value an identifier asks for.</summary>
/// <param name="Name">The constructor parameter's name.</param>
/// <param name="TypeName">Its type, as it would be written in source.</param>
public sealed record IdentifierParameter(string Name, string TypeName);

/// <summary>The outcome of building an identifier.</summary>
/// <param name="Instance">The identifier, or null when it could not be built.</param>
/// <param name="Error">Why not, or null when it was.</param>
public sealed record CreatedIdentifier(object? Instance, string? Error);
