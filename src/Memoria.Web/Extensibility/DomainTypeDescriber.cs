using System.Reflection;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads what can usefully be said about one domain type, for the detail panel beside a list.
/// </summary>
public static class DomainTypeDescriber
{
    /// <summary>
    /// Picks the type a page was asked for out of the ones it is listing.
    /// </summary>
    /// <param name="types">The types on offer.</param>
    /// <param name="fullName">The full name asked for, or null when nothing is selected.</param>
    /// <returns>The type, or null when nothing was asked for or the name is not among them.</returns>
    /// <remarks>
    /// Matched against the list rather than resolved from the name, so a name arriving in a query
    /// string can only ever select something already on the page.
    /// </remarks>
    public static Type? Select(IReadOnlyList<Type> types, string? fullName) =>
        string.IsNullOrWhiteSpace(fullName)
            ? null
            : types.FirstOrDefault(type => type.FullName == fullName);

    /// <summary>
    /// Describes one aggregate or projection.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <param name="identifiers">The identifier types to look through for ones addressing it.</param>
    public static DomainTypeDescription Describe(Type type, IReadOnlyList<Type> identifiers) =>
        new(
            type,
            BindingKeyOf(type),
            type.Assembly.GetName().Name ?? "unknown",
            identifiers.Where(identifier => Addresses(identifier, type)).ToList(),
            EventTypesOf(type),
            PropertiesOf(type));

    private static string? BindingKeyOf(Type type) =>
        type.GetCustomAttribute<AggregateType>() is { } aggregate
            ? TypeBindings.GetTypeBindingKey(aggregate.Name, aggregate.Version)
            : type.GetCustomAttribute<ProjectionType>() is { } projection
                ? TypeBindings.GetTypeBindingKey(projection.Name, projection.Version)
                : type.GetCustomAttribute<EventType>() is { } @event
                    ? TypeBindings.GetTypeBindingKey(@event.Name, @event.Version)
                    : null;

    /// <summary>
    /// Whether an identifier is the identifier of this model, read off the generic argument of the
    /// identifier interface it closes — <c>IDcbAggregateId&lt;Product&gt;</c> and its streamed and
    /// projection counterparts.
    /// </summary>
    private static bool Addresses(Type identifier, Type model) =>
        identifier.GetInterfaces().Any(@interface =>
            @interface.IsGenericType &&
            IdentifierInterfaces.Contains(@interface.GetGenericTypeDefinition()) &&
            @interface.GetGenericArguments()[0] == model);

    private static readonly Type[] IdentifierInterfaces =
    [
        typeof(IAggregateId<>), typeof(IProjectionId<>),
        typeof(IDcbAggregateId<>), typeof(IDcbProjectionId<>)
    ];

    /// <summary>
    /// The events the model applies, taken from its own <c>EventTypeFilter</c>.
    /// </summary>
    /// <remarks>
    /// Reading it means constructing one, because the filter is an instance property. That is how
    /// the store reads it too, so a model that cannot be constructed here could not be folded
    /// either — but this is a page, so a model that throws costs its event list, not the page.
    /// </remarks>
    private static IReadOnlyList<Type> EventTypesOf(Type type)
    {
        try
        {
            return InstanceFactory.CreateInstance(type) is EventSourcedModel model
                ? model.EventTypeFilter ?? []
                : [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// Reads the properties a type declares itself.
    /// </summary>
    /// <param name="type">The type to read. Any type — a model, or an event carrying state.</param>
    /// <remarks>
    /// Inherited ones are the framework's own — Version, StreamId and the rest — and say nothing
    /// about this type. Nor does an override of one: EventTypeFilter is declared on a model but
    /// belongs to the framework, and is already shown as the events the model applies.
    /// </remarks>
    public static IReadOnlyList<DomainProperty> PropertiesOf(Type type) =>
        Declared(type)
            .Select(property => new DomainProperty(property.Name, Readable(property.PropertyType)))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Reads the state off a model that has been loaded, through the same filter the description
    /// uses, so the page shows the same properties with values against them.
    /// </summary>
    /// <param name="model">The loaded aggregate or projection.</param>
    public static IReadOnlyList<DomainPropertyValue> ReadState(object model) =>
        Declared(model.GetType())
            .Select(property => new DomainPropertyValue(
                property.Name,
                Readable(property.PropertyType),
                Read(property, model)))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToList();

    private static string? Read(PropertyInfo property, object model)
    {
        try
        {
            return property.GetValue(model) switch
            {
                null => null,
                IEnumerable<object> many => string.Join(", ", many),
                var value => value.ToString()
            };
        }
        catch (Exception exception)
        {
            // A getter that throws is the model's business, not a reason to lose the page.
            return $"could not be read: {exception.Message}";
        }
    }

    private static IEnumerable<PropertyInfo> Declared(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => !Overrides(property));

    /// <summary>
    /// Whether a property is an override of one declared further up, rather than the model's own.
    /// </summary>
    private static bool Overrides(PropertyInfo property)
    {
        var accessor = property.GetMethod ?? property.SetMethod;

        return accessor is not null &&
               accessor.GetBaseDefinition().DeclaringType != accessor.DeclaringType;
    }

    /// <summary>Spells a type the way it would be written in source.</summary>
    internal static string Readable(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{Readable(underlying)}?";
        }

        if (Aliases.TryGetValue(type, out var alias))
        {
            return alias;
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = string.Join(", ", type.GetGenericArguments().Select(Readable));

        return $"{name}<{arguments}>";
    }

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(string)] = "string", [typeof(bool)] = "bool", [typeof(byte)] = "byte",
        [typeof(int)] = "int", [typeof(long)] = "long", [typeof(short)] = "short",
        [typeof(decimal)] = "decimal", [typeof(double)] = "double", [typeof(float)] = "float",
        [typeof(object)] = "object", [typeof(char)] = "char", [typeof(Guid)] = "Guid"
    };
}

/// <summary>
/// What one page shows about a single domain type.
/// </summary>
/// <param name="Type">The type itself.</param>
/// <param name="BindingKey">The <c>name:version</c> it is stored under, or null if it carries no attribute.</param>
/// <param name="AssemblyName">The assembly it was uploaded in.</param>
/// <param name="Identifiers">The identifier types that address it.</param>
/// <param name="EventTypes">The events it applies.</param>
/// <param name="Properties">The properties it declares.</param>
public sealed record DomainTypeDescription(
    Type Type,
    string? BindingKey,
    string AssemblyName,
    IReadOnlyList<Type> Identifiers,
    IReadOnlyList<Type> EventTypes,
    IReadOnlyList<DomainProperty> Properties);

/// <summary>One declared property.</summary>
/// <param name="Name">Its name.</param>
/// <param name="TypeName">Its type, as it would be written in source.</param>
public sealed record DomainProperty(string Name, string TypeName);

/// <summary>One declared property, with what it currently holds.</summary>
/// <param name="Name">Its name.</param>
/// <param name="TypeName">Its type, as it would be written in source.</param>
/// <param name="Value">What it holds, or null.</param>
public sealed record DomainPropertyValue(string Name, string TypeName, string? Value);
