using System.Reflection;

namespace OpenCqrs.EventSourcing.Domain;

/// <summary>
/// Marker interface that identifies domain events in the event sourcing system.
/// Domain events represent significant business occurrences that have happened in the domain
/// and are stored as part of the event stream.
/// </summary>
/// <example>
/// <code>
/// // Simple domain event
/// [DomainEventType("OrderPlaced", 1)]
/// public record OrderPlacedEvent : IDomainEvent
/// {
///     public Guid OrderId { get; init; }
///     public Guid CustomerId { get; init; }
///     public DateTime PlacedAt { get; init; }
///     public decimal TotalAmount { get; init; }
///     public List&lt;OrderItem&gt; Items { get; init; } = new();
/// }
/// 
/// // Event with business context
/// [DomainEventType("PaymentProcessed", 1)]
/// public record PaymentProcessedEvent : IDomainEvent
/// {
///     public Guid PaymentId { get; init; }
///     public Guid OrderId { get; init; }
///     public decimal Amount { get; init; }
///     public string PaymentMethod { get; init; }
///     public DateTime ProcessedAt { get; init; }
///     public string TransactionId { get; init; }
/// }
/// 
/// // Usage in aggregates
/// public class Order : Aggregate
/// {
///     public void PlaceOrder(CustomerId customerId, List&lt;OrderItem&gt; items)
///     {
///         var orderPlaced = new OrderPlacedEvent
///         {
///             OrderId = Id,
///             CustomerId = customerId.Id,
///             PlacedAt = DateTime.UtcNow,
///             Items = items,
///             TotalAmount = items.Sum(i =&gt; i.Price * i.Quantity)
///         };
///         
///         Add(orderPlaced); // Adds to uncommitted events
///     }
/// }
/// </code>
/// </example>
public interface IDomainEvent;

/// <summary>
/// Attribute that provides type metadata for domain events, including logical name and version information.
/// This metadata is essential for event serialization, deserialization, and schema evolution in event stores.
/// </summary>
/// <param name="name">
/// The logical name of the domain event type. This should be a stable identifier that remains consistent
/// even if the C# class name changes. Used for event type identification during serialization/deserialization.
/// </param>
/// <param name="version">
/// The version number of the domain event schema. Defaults to 1. Used for managing schema evolution
/// and ensuring proper deserialization of events stored with different versions.
/// </param>
/// <example>
/// <code>
/// // Basic event with default version
/// [DomainEventType("UserRegistered")]
/// public record UserRegisteredEvent : IDomainEvent
/// {
///     public string UserId { get; init; }
///     public string Email { get; init; }
///     public DateTime RegisteredAt { get; init; }
/// }
/// 
/// // Versioned event for schema evolution
/// [DomainEventType("OrderPlaced", 2)]
/// public record OrderPlacedEventV2 : IDomainEvent
/// {
///     public Guid OrderId { get; init; }
///     public Guid CustomerId { get; init; }
///     public DateTime PlacedAt { get; init; }
///     public decimal TotalAmount { get; init; }
///     public string Currency { get; init; } = "USD"; // New in v2
///     public List&lt;OrderItem&gt; Items { get; init; } = new();
/// }
/// 
/// // Event name different from class name
/// [DomainEventType("customer-address-changed", 1)]
/// public record CustomerAddressUpdatedEvent : IDomainEvent
/// {
///     public Guid CustomerId { get; init; }
///     public Address OldAddress { get; init; }
///     public Address NewAddress { get; init; }
///     public DateTime ChangedAt { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class DomainEventType(string name, byte version = 1) : Attribute
{
    /// <summary>
    /// Gets the logical name of the domain event type.
    /// </summary>
    /// <value>
    /// A string that serves as the stable, logical identifier for this event type.
    /// This name is used for serialization and should remain constant even if the class name changes.
    /// </value>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the version number of the domain event schema.
    /// </summary>
    /// <value>
    /// A byte value representing the schema version of this event type.
    /// Used for managing schema evolution and compatibility during event deserialization.
    /// </value>
    public byte Version { get; } = version;
}

/// <summary>
/// Provides extension methods for <see cref="IDomainEvent"/> to extract type metadata and support
/// event serialization, versioning, and type binding operations.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Extracts the <see cref="DomainEventType"/> attribute information from a domain event instance.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to extract type information from.</param>
    /// <returns>
    /// The <see cref="DomainEventType"/> attribute containing the event's logical name and version.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the domain event class does not have a <see cref="DomainEventType"/> attribute.
    /// </exception>
    /// <example>
    /// <code>
    /// [DomainEventType("OrderPlaced", 2)]
    /// public record OrderPlacedEvent : IDomainEvent
    /// {
    ///     public Guid OrderId { get; init; }
    ///     public DateTime PlacedAt { get; init; }
    /// }
    /// 
    /// // Usage
    /// var orderEvent = new OrderPlacedEvent { OrderId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow };
    /// var eventType = orderEvent.DomainEventType();
    /// 
    /// Console.WriteLine($"Event Name: {eventType.Name}");    // "OrderPlaced"
    /// Console.WriteLine($"Event Version: {eventType.Version}"); // 2
    /// 
    /// // In serialization scenarios
    /// public string SerializeEvent(IDomainEvent domainEvent)
    /// {
    ///     var eventType = domainEvent.DomainEventType();
    ///     var metadata = new EventMetadata
    ///     {
    ///         EventType = eventType.Name,
    ///         Version = eventType.Version,
    ///         Timestamp = DateTime.UtcNow
    ///     };
    ///     
    ///     return JsonSerializer.Serialize(new { Metadata = metadata, Data = domainEvent });
    /// }
    /// </code>
    /// </example>
    public static DomainEventType DomainEventType(this IDomainEvent domainEvent)
    {
        var domainEventType = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventType == null)
        {
            throw new Exception($"Domain Event {domainEvent.GetType().Name} does not have an DomainEventType attribute.");
        }
        return domainEventType;
    }

    /// <summary>
    /// Generates a type binding key for the domain event that combines the event name and version
    /// into a format suitable for type resolution and serialization mapping.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to generate a binding key for.</param>
    /// <returns>
    /// A string that uniquely identifies the event type and version combination,
    /// suitable for use in type binding registrations and serialization scenarios.
    /// </returns>
    /// <example>
    /// <code>
    /// [DomainEventType("OrderPlaced", 2)]
    /// public record OrderPlacedEvent : IDomainEvent
    /// {
    ///     public Guid OrderId { get; init; }
    /// }
    /// 
    /// // Usage
    /// var orderEvent = new OrderPlacedEvent { OrderId = Guid.NewGuid() };
    /// var bindingKey = orderEvent.GetTypeBindingKey(); // e.g., "OrderPlaced_v2"
    /// 
    /// // In type registration
    /// public void RegisterEventTypes(IServiceCollection services)
    /// {
    ///     var eventTypes = Assembly.GetExecutingAssembly()
    ///         .GetTypes()
    ///         .Where(t =&gt; typeof(IDomainEvent).IsAssignableFrom(t))
    ///         .ToList();
    ///     
    ///     foreach (var eventType in eventTypes)
    ///     {
    ///         var instance = Activator.CreateInstance(eventType) as IDomainEvent;
    ///         var bindingKey = instance.GetTypeBindingKey();
    ///         
    ///         // Register type mapping
    ///         TypeBindings.Register(bindingKey, eventType);
    ///     }
    /// }
    /// 
    /// // In event store operations
    /// public async Task SaveEventAsync(IDomainEvent domainEvent)
    /// {
    ///     var typeKey = domainEvent.GetTypeBindingKey();
    ///     var eventData = new StoredEvent
    ///     {
    ///         EventType = typeKey,
    ///         Data = JsonSerializer.Serialize(domainEvent),
    ///         Timestamp = DateTime.UtcNow
    ///     };
    ///     
    ///     await eventStore.AppendAsync(eventData);
    /// }
    /// </code>
    /// </example>
    public static string GetTypeBindingKey(this IDomainEvent domainEvent)
    {
        var domainEventType = domainEvent.DomainEventType();
        return TypeBindings.GetTypeBindingKey(domainEventType.Name, domainEventType.Version);
    }
}
