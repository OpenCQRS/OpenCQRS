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
            BindingOf(type),
            type.Assembly.GetName().Name ?? "unknown",
            IdentifiersOf(type, identifiers),
            EventTypesOf(type),
            PropertiesOf(type));

    /// <summary>
    /// The identifiers that address one model.
    /// </summary>
    /// <param name="type">The model they would address.</param>
    /// <param name="identifiers">The identifier types to look through.</param>
    /// <remarks>
    /// Reachable for a bare type, for the same reason the binding is: a list showing what each of
    /// its rows can be loaded by wants this and nothing else, and describing every row in full
    /// constructs every model to read its event filter.
    /// </remarks>
    public static IReadOnlyList<Type> IdentifiersOf(Type type, IReadOnlyList<Type> identifiers) =>
        identifiers.Where(identifier => Addresses(identifier, type)).ToList();

    /// <summary>
    /// Reads what a type is bound as, off whichever of the three attributes it carries.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The binding, or null when the type carries none of them.</returns>
    /// <remarks>
    /// Reachable for a bare type, so a list can label its rows by what they are bound as without
    /// describing each one in full — describing constructs the model to read its event filter, and
    /// a list has no use for that.
    /// </remarks>
    public static DomainTypeBinding? BindingOf(Type type) =>
        type.GetCustomAttribute<AggregateType>() is { } aggregate
            ? new DomainTypeBinding(aggregate.Name, aggregate.Version)
            : type.GetCustomAttribute<ProjectionType>() is { } projection
                ? new DomainTypeBinding(projection.Name, projection.Version)
                : type.GetCustomAttribute<EventType>() is { } @event
                    ? new DomainTypeBinding(@event.Name, @event.Version)
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
    /// Reads the properties a type declares itself, and the properties of the values they hold.
    /// </summary>
    /// <param name="type">The type to read. Any type — a model, or an event carrying state.</param>
    /// <remarks>
    /// <para>
    /// Inherited ones are the framework's own — Version, StreamId and the rest — and say nothing
    /// about this type. Nor does an override of one: EventTypeFilter is declared on a model but
    /// belongs to the framework, and is already shown as the events the model applies.
    /// </para>
    /// <para>
    /// A property holding a value of the domain's own is read through as well, because the type
    /// name alone answers nothing: <c>PackagedSize</c> in a type column tells a reader there is a
    /// shape and not what is in it. What that costs is bounded by <see cref="Depth"/> and by
    /// refusing to walk back into a type already being walked.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<DomainProperty> PropertiesOf(Type type) => PropertiesOf(type, []);

    /// <summary>
    /// How many values deep to read. Enough for a value holding a value, which is as far as the
    /// shapes on these pages go, and short enough that a type column stays a column.
    /// </summary>
    private const int Depth = 3;

    /// <param name="unfolded">
    /// The types already being read through on the way here. A value that holds its own kind would
    /// otherwise be unfolded forever, and one that holds it twice would be read twice over.
    /// </param>
    private static IReadOnlyList<DomainProperty> PropertiesOf(
        Type type, IReadOnlyList<Type> unfolded) =>
        Declared(type)
            .Select(property => Describe(property, unfolded))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToList();

    private static DomainProperty Describe(PropertyInfo property, IReadOnlyList<Type> unfolded)
    {
        var held = Unfoldable(property.PropertyType);

        // Left folded rather than dropped when the walk has to stop: the property is still part of
        // the shape, and a reader who wants what is under it can select that type on its own page.
        var children = held is null || unfolded.Count == Depth || unfolded.Contains(held)
            ? []
            : PropertiesOf(held, [..unfolded, held]);

        return new DomainProperty(property.Name, Readable(property.PropertyType), children);
    }

    /// <summary>
    /// The type whose properties describe a value, or null when the value has none worth reading.
    /// </summary>
    /// <remarks>
    /// A collection is described by what it holds. The list type itself is already spelled out in
    /// the type column, so unfolding it would repeat that and say nothing else.
    /// </remarks>
    private static Type? Unfoldable(Type type)
    {
        var held = Nullable.GetUnderlyingType(type) ?? type;

        return ElementOf(held) is { } element ? Unfoldable(element) : Composite(held) ? held : null;
    }

    /// <summary>What a collection holds, or null when the type is not one.</summary>
    /// <remarks>
    /// A string is a sequence of characters and is never meant as one here, so it is turned away
    /// before anything else.
    /// </remarks>
    private static Type? ElementOf(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return type.GetInterfaces()
            .Prepend(type)
            .FirstOrDefault(candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Whether a value is one of the domain's own, with a shape worth reading.
    /// </summary>
    /// <remarks>
    /// Everything the framework provides is turned away by its namespace rather than by a list of
    /// types, so a page cannot be filled with <c>DateTimeOffset</c>'s dozen properties by a domain
    /// that happens to use one this codebase never thought to name. An enum has no state to read;
    /// its members are values of the type, not properties of an instance.
    /// </remarks>
    private static bool Composite(Type type) =>
        !type.IsPrimitive && !type.IsEnum && !Aliases.ContainsKey(type) && !Framework(type);

    private static bool Framework(Type type) =>
        type.Namespace is { } space &&
        (space == "System" || space.StartsWith("System.", StringComparison.Ordinal) ||
         space == "Microsoft" || space.StartsWith("Microsoft.", StringComparison.Ordinal));

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
/// <param name="Binding">What it is stored under, or null if it carries no attribute.</param>
/// <param name="AssemblyName">The assembly it was uploaded in.</param>
/// <param name="Identifiers">The identifier types that address it.</param>
/// <param name="EventTypes">The events it applies.</param>
/// <param name="Properties">The properties it declares.</param>
public sealed record DomainTypeDescription(
    Type Type,
    DomainTypeBinding? Binding,
    string AssemblyName,
    IReadOnlyList<Type> Identifiers,
    IReadOnlyList<Type> EventTypes,
    IReadOnlyList<DomainProperty> Properties);

/// <summary>
/// What a type is bound as: the stable name the store writes it under, and the schema version of
/// that name. Kept apart because they are two facts about the type, and only the store needs them
/// as one string.
/// </summary>
/// <param name="Name">The logical name, which outlives a rename of the class.</param>
/// <param name="Version">The schema version of that name.</param>
public sealed record DomainTypeBinding(string Name, byte Version)
{
    /// <summary>Gets the two as the single key the store keeps them under.</summary>
    public string Key => TypeBindings.GetTypeBindingKey(Name, Version);
}

/// <summary>One declared property.</summary>
/// <param name="Name">Its name.</param>
/// <param name="TypeName">Its type, as it would be written in source.</param>
/// <param name="Children">
/// The properties of the value it holds, or of what its collection holds. Empty for a number, a
/// string, an enum, anything the framework provides, and for a value the walk had to stop on.
/// </param>
public sealed record DomainProperty(
    string Name, string TypeName, IReadOnlyList<DomainProperty> Children)
{
    /// <summary>A property with nothing under it.</summary>
    public DomainProperty(string name, string typeName) : this(name, typeName, [])
    {
    }
}

/// <summary>One declared property, with what it currently holds.</summary>
/// <param name="Name">Its name.</param>
/// <param name="TypeName">Its type, as it would be written in source.</param>
/// <param name="Value">What it holds, or null.</param>
public sealed record DomainPropertyValue(string Name, string TypeName, string? Value);
