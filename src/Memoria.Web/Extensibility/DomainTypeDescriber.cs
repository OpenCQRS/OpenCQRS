using System.Collections;
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
    /// How a type is named wherever a page lists one: what it is bound as, and the version of that
    /// binding after it. A type carrying no attribute is named by its class, which is all there is
    /// to name it by.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <remarks>
    /// Written as a name and a version rather than as the store's own key: the key joins the two
    /// with a colon because something has to read them back apart, which is the store's need and
    /// not the reader's. Here they are a name and its version, said the way a version is said. The
    /// key itself belongs where a page is saying what the store holds rather than what a type is.
    /// <para>
    /// One name for all of them, because a type met on a list, on the panel beside it and in a
    /// column of stored rows is the same type, and reading it two ways would make it look like two.
    /// </para>
    /// </remarks>
    public static string LabelOf(Type type) =>
        BindingOf(type) is { } binding ? Label(binding.Name, binding.Version) : type.Name;

    /// <summary>
    /// The same name, with a word after it when the type is retired, for a control that can hold
    /// text and nothing else — an option in a select, which has no room for the mark every other
    /// list carries and would otherwise be the one place a retired type looked current.
    /// </summary>
    /// <param name="type">The type to name.</param>
    public static string OptionLabelOf(Type type) =>
        ObsoleteOf(type) is null ? LabelOf(type) : $"{LabelOf(type)} (obsolete)";

    /// <summary>
    /// That a type is retired, in the words of its own <see cref="ObsoleteAttribute"/>, or null
    /// when it is not.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>
    /// "Obsolete", then the attribute's message after a dash when it carries one — so a reader
    /// meets what the sentence is about before the reason — and null for a type carrying no such
    /// attribute.
    /// </returns>
    /// <remarks>
    /// Retired is not the same as old. A type with a later version beside it is old, and the
    /// version says so; a retired type is one nothing should write through any more, whether or
    /// not anything replaced it, and the attribute is the only place the domain says that. It stays
    /// bound all the same: the rows it wrote are still in the store, and only its own shape can
    /// read them back.
    /// </remarks>
    public static string? ObsoleteOf(Type type) =>
        type.GetCustomAttribute<ObsoleteAttribute>() is { } obsolete
            ? string.IsNullOrWhiteSpace(obsolete.Message) ? "Obsolete" : $"Obsolete — {obsolete.Message}"
            : null;

    /// <summary>
    /// The same name, worked out from a stored key rather than from a type — which is what a page
    /// saying what the store holds has to hand.
    /// </summary>
    /// <param name="key">The key, as <c>name:version</c>.</param>
    /// <remarks>
    /// A key whose second half is not a number is not a key this knows how to take apart, so it is
    /// left whole rather than half-read: whatever the store turns out to hold is worth seeing as it
    /// holds it.
    /// </remarks>
    public static string LabelOfKey(string key) =>
        SplitKey(key) is { Version: { } version } split && int.TryParse(version, out var numbered)
            ? Label(split.Name, numbered)
            : key;

    /// <summary>
    /// A name and the version of it, with the version said only when it is one of several.
    /// </summary>
    /// <remarks>
    /// A first version is what a name means until a second one exists, so saying it adds nothing —
    /// and a page whose every row ends in the same two characters has taught its reader to skip
    /// them, which is the opposite of what a version beside a name is for. The moment there is a
    /// second, both are worth telling apart and only one of them is silent.
    /// </remarks>
    private static string Label(string name, int version) =>
        version > 1 ? $"{name} v{version}" : name;

    /// <summary>
    /// Splits a stored key into the two halves it is made of.
    /// </summary>
    /// <param name="key">The key, as <c>name:version</c>.</param>
    /// <returns>The name, and the version when the key carries one.</returns>
    /// <remarks>
    /// Split at the last separator: the two are joined with nothing escaped, so a name carrying a
    /// colon of its own still leaves the version after the last one. A key carrying no separator at
    /// all is a name with nothing to say beside it rather than one that cannot be read — whatever
    /// the store turns out to hold is shown.
    /// </remarks>
    public static (string Name, string? Version) SplitKey(string key) =>
        key.LastIndexOf(':') is var separator && separator < 0
            ? (key, null)
            : (key[..separator], key[(separator + 1)..]);

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
    public static IReadOnlyList<DomainPropertyValue> ReadState(object model) => ReadState(model, []);

    private static IReadOnlyList<DomainPropertyValue> ReadState(
        object model, IReadOnlyList<Type> unfolded) =>
        Declared(model.GetType())
            .Select(property => Read(property, model, unfolded))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToList();

    private static DomainPropertyValue Read(
        PropertyInfo property, object model, IReadOnlyList<Type> unfolded)
    {
        var typeName = Readable(property.PropertyType);

        try
        {
            return Read(property.Name, typeName, property.GetValue(model), unfolded);
        }
        catch (Exception exception)
        {
            // A getter that throws is the model's business, not a reason to lose the page.
            return new DomainPropertyValue(
                property.Name, typeName, $"could not be read: {exception.Message}");
        }
    }

    /// <summary>
    /// Reads one value: as a line of its own when there is nothing inside it, and as what is inside
    /// it when there is.
    /// </summary>
    /// <remarks>
    /// The value read is the one actually held rather than the one the property is declared as, so
    /// what comes back is what the model is really carrying. The declared type stays in
    /// <paramref name="typeName"/>, because that is the shape the model promises and it is what the
    /// same property is listed under where there is no instance to read.
    /// </remarks>
    private static DomainPropertyValue Read(
        string name, string typeName, object? value, IReadOnlyList<Type> unfolded)
    {
        if (value is null)
        {
            return new DomainPropertyValue(name, typeName, null);
        }

        if (value is not string && value is IEnumerable items)
        {
            return ReadList(name, typeName, items, unfolded);
        }

        var held = value.GetType();

        // Left as whatever the value says about itself when the walk has to stop, rather than
        // dropped: one line of a record's own printout is worse than the shape underneath it and
        // better than an empty cell.
        return Composite(held) && unfolded.Count < Depth && !unfolded.Contains(held)
            ? new DomainPropertyValue(name, typeName, null, ReadState(value, [..unfolded, held]))
            : new DomainPropertyValue(name, typeName, value.ToString());
    }

    /// <summary>
    /// Reads what a collection holds: on one line when its elements have no shape of their own, and
    /// an element to a row when they have.
    /// </summary>
    /// <remarks>
    /// Read through the untyped <see cref="IEnumerable"/> so that a list of numbers is read the same
    /// way as a list of strings. <c>IEnumerable&lt;object&gt;</c> would take the second and not the
    /// first: variance carries reference types and leaves value types behind, so a list of numbers
    /// matched nothing and reported its own class name instead of what was in it.
    /// </remarks>
    private static DomainPropertyValue ReadList(
        string name, string typeName, IEnumerable items, IReadOnlyList<Type> unfolded)
    {
        var entries = items.Cast<object?>().ToList();

        if (entries.Count == 0)
        {
            return new DomainPropertyValue(name, typeName, null);
        }

        if (entries.All(entry => entry is null || !Composite(entry.GetType())))
        {
            return new DomainPropertyValue(
                name, typeName, string.Join(", ", entries.Select(entry => entry?.ToString())));
        }

        // Numbered rather than named, because an element of a list has no name of its own — and
        // left in the order the model holds them in, which is the only order they have.
        return new DomainPropertyValue(
            name,
            typeName,
            $"{entries.Count} item{(entries.Count == 1 ? string.Empty : "s")}",
            [
                ..entries.Select((entry, index) => Read(
                    $"[{index}]", Readable(entry?.GetType() ?? typeof(object)), entry, unfolded))
            ]);
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
/// <param name="Value">
/// What it holds, or null when it holds nothing — and null as well when it holds a shape, because
/// then what it holds is in <paramref name="Children"/> and a record's own printout beside them
/// would be the same thing said twice. For a list of shapes it is how many there are.
/// </param>
/// <param name="Children">
/// What is inside the value: the properties of a shape, or one entry per element of a list of
/// shapes, named by position. Empty for anything that reads as a line of its own.
/// </param>
public sealed record DomainPropertyValue(
    string Name, string TypeName, string? Value, IReadOnlyList<DomainPropertyValue> Children)
{
    /// <summary>A property whose value is a line of its own.</summary>
    public DomainPropertyValue(string name, string typeName, string? value)
        : this(name, typeName, value, [])
    {
    }
}
