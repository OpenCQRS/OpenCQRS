using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Data;

/// <summary>
/// Defines the contract for entities that support type binding functionality in the Entity Framework Core event store.
/// Enables proper serialization, deserialization, and type resolution by maintaining logical type names and versions
/// that are decoupled from actual .NET class names, supporting schema evolution and cross-platform compatibility.
/// </summary>
/// <example>
/// <code>
/// // Example entity implementing IBindableEntity
/// public class OrderEventEntity : IBindableEntity
/// {
///     public string Id { get; set; } = null!;
///     public string StreamId { get; set; } = null!;
///     public string Data { get; set; } = null!;
///     
///     // Type binding properties
///     public string TypeName { get; set; } = null!;
///     public int TypeVersion { get; set; }
/// }
/// 
/// // Corresponding domain event with type attribute
/// [DomainEventType("OrderPlaced", 1)]
/// public record OrderPlacedEvent : IDomainEvent
/// {
///     public Guid OrderId { get; init; }
///     public decimal Amount { get; init; }
///     public DateTime PlacedAt { get; init; }
/// }
/// 
/// // Type binding registration during application startup
/// public void RegisterTypeBindings()
/// {
///     TypeBindings.DomainEventTypeBindings.Add("OrderPlaced_v1", typeof(OrderPlacedEvent));
/// }
/// 
/// // Usage in serialization
/// public OrderEventEntity SerializeEvent(OrderPlacedEvent domainEvent)
/// {
///     var eventTypeAttribute = typeof(OrderPlacedEvent).GetCustomAttribute&lt;DomainEventTypeAttribute&gt;();
///     
///     return new OrderEventEntity
///     {
///         Id = Guid.NewGuid().ToString(),
///         StreamId = "order-stream-123",
///         Data = JsonConvert.SerializeObject(domainEvent),
///         TypeName = eventTypeAttribute.Name,        // "OrderPlaced"
///         TypeVersion = eventTypeAttribute.Version   // 1
///     };
/// }
/// 
/// // Usage in deserialization
/// public OrderPlacedEvent DeserializeEvent(OrderEventEntity entity)
/// {
///     var bindingKey = entity.GetTypeBindingKey(); // "OrderPlaced_v1"
///     var eventType = entity.ToDomainEventType();  // typeof(OrderPlacedEvent)
///     
///     return (OrderPlacedEvent)JsonConvert.DeserializeObject(entity.Data, eventType);
/// }
/// 
/// // Schema evolution example - Version 2 of OrderPlaced event
/// [DomainEventType("OrderPlaced", 2)]
/// public record OrderPlacedEventV2 : IDomainEvent
/// {
///     public Guid OrderId { get; init; }
///     public decimal Amount { get; init; }
///     public DateTime PlacedAt { get; init; }
///     public string Currency { get; init; } = "USD"; // New property in v2
///     public List&lt;string&gt; Tags { get; init; } = new(); // New property in v2
/// }
/// 
/// // Register both versions
/// public void RegisterAllVersions()
/// {
///     TypeBindings.DomainEventTypeBindings.Add("OrderPlaced_v1", typeof(OrderPlacedEvent));
///     TypeBindings.DomainEventTypeBindings.Add("OrderPlaced_v2", typeof(OrderPlacedEventV2));
/// }
/// 
/// // Version-aware processing
/// public IDomainEvent ProcessStoredEvent(OrderEventEntity entity)
/// {
///     return entity.TypeVersion switch
///     {
///         1 =&gt; DeserializeAs&lt;OrderPlacedEvent&gt;(entity),
///         2 =&gt; DeserializeAs&lt;OrderPlacedEventV2&gt;(entity),
///         _ =&gt; throw new NotSupportedException($"Unsupported version {entity.TypeVersion} for {entity.TypeName}")
///     };
/// }
/// </code>
/// </example>
public interface IBindableEntity
{
    /// <summary>
    /// Gets or sets the logical name of the type as defined in the corresponding type attribute.
    /// This name decouples the storage from actual .NET class names, enabling safe refactoring
    /// and cross-platform compatibility in the event sourcing system.
    /// </summary>
    /// <value>
    /// A string representing the logical name of the type. Should match the Name property
    /// from the corresponding <see cref="DomainEventType"/> or <see cref="AggregateType"/> attribute
    /// on the associated domain object.
    /// </value>
    string TypeName { get; set; }

    /// <summary>
    /// Gets or sets the version number of the type schema as defined in the corresponding type attribute.
    /// Supports schema evolution by enabling multiple versions of the same logical type to coexist
    /// and be properly deserialized in the event sourcing system.
    /// </summary>
    /// <value>
    /// An integer representing the schema version of the type. Should match the Version property
    /// from the corresponding <see cref="DomainEventType"/> or <see cref="AggregateType"/> attribute
    /// on the associated domain object.
    /// </value>
    int TypeVersion { get; set; }
}

/// <summary>
/// Provides extension methods for <see cref="IBindableEntity"/> to support type binding operations
/// in the event sourcing infrastructure. These extensions enable type resolution, binding key generation,
/// and integration with the global type binding system for proper serialization and deserialization.
/// </summary>
public static class BindableEntityExtensions
{
    /// <summary>
    /// Generates a standardized type binding key from the entity's type name and version information.
    /// This key is used for consistent type lookups in the global type binding registries throughout
    /// the event sourcing system.
    /// </summary>
    /// <param name="bindableEntity">
    /// The entity implementing <see cref="IBindableEntity"/> containing type name and version information.
    /// </param>
    /// <returns>
    /// A formatted string that serves as a unique key for type binding operations,
    /// combining the type name and version in a standardized format.
    /// </returns>
    /// <example>
    /// <code>
    /// // Example entity with type binding information
    /// var eventEntity = new EventEntity
    /// {
    ///     TypeName = "OrderPlaced",
    ///     TypeVersion = 1,
    ///     Data = "{\"OrderId\":\"123\",\"Amount\":199.99}"
    /// };
    /// 
    /// // Generate binding key for type lookup
    /// string bindingKey = eventEntity.GetTypeBindingKey();
    /// // Result: "OrderPlaced_v1" (exact format depends on TypeBindings implementation)
    /// 
    /// // Use key for manual type lookup
    /// if (TypeBindings.DomainEventTypeBindings.ContainsKey(bindingKey))
    /// {
    ///     var eventType = TypeBindings.DomainEventTypeBindings[bindingKey];
    ///     var domainEvent = JsonConvert.DeserializeObject(eventEntity.Data, eventType);
    /// }
    /// 
    /// // Key generation for different versions of the same type
    /// var eventV1 = new EventEntity { TypeName = "OrderPlaced", TypeVersion = 1 };
    /// var eventV2 = new EventEntity { TypeName = "OrderPlaced", TypeVersion = 2 };
    /// 
    /// string keyV1 = eventV1.GetTypeBindingKey(); // "OrderPlaced_v1"
    /// string keyV2 = eventV2.GetTypeBindingKey(); // "OrderPlaced_v2"
    /// 
    /// // Keys are different, enabling version-specific type resolution
    /// Debug.Assert(keyV1 != keyV2);
    /// 
    /// // Usage in generic type binding operations
    /// public void ProcessBindableEntity&lt;T&gt;(T entity) where T : IBindableEntity
    /// {
    ///     var bindingKey = entity.GetTypeBindingKey();
    ///     _logger.LogDebug("Processing entity with binding key: {BindingKey}", bindingKey);
    ///     
    ///     // Proceed with type-specific processing...
    /// }
    /// 
    /// // Batch key generation for multiple entities
    /// public Dictionary&lt;string, List&lt;IBindableEntity&gt;&gt; GroupEntitiesByType(
    ///     List&lt;IBindableEntity&gt; entities)
    /// {
    ///     return entities
    ///         .GroupBy(e =&gt; e.GetTypeBindingKey())
    ///         .ToDictionary(g =&gt; g.Key, g =&gt; g.ToList());
    /// }
    /// </code>
    /// </example>
    public static string GetTypeBindingKey(this IBindableEntity bindableEntity) =>
        TypeBindings.GetTypeBindingKey(bindableEntity.TypeName, bindableEntity.TypeVersion);

    /// <summary>
    /// Resolves the entity's type binding information to the corresponding .NET <see cref="Type"/> for domain events.
    /// Uses the global domain event type binding registry to find the actual type that should be used
    /// for deserialization operations.
    /// </summary>
    /// <param name="bindableEntity">
    /// The entity implementing <see cref="IBindableEntity"/> containing type name and version information.
    /// </param>
    /// <returns>
    /// The .NET <see cref="Type"/> that corresponds to the entity's type binding information,
    /// suitable for use in deserialization and type-specific processing operations.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type specified by the entity's <see cref="IBindableEntity.TypeName"/> and
    /// <see cref="IBindableEntity.TypeVersion"/> is not found in the <see cref="TypeBindings.DomainEventTypeBindings"/>
    /// registry. This typically indicates that the type has not been properly registered during application startup.
    /// </exception>
    /// <example>
    /// <code>
    /// // Example domain event entity
    /// var eventEntity = new EventEntity
    /// {
    ///     TypeName = "OrderPlaced",
    ///     TypeVersion = 1,
    ///     Data = "{\"OrderId\":\"123\",\"Amount\":199.99}"
    /// };
    /// 
    /// try
    /// {
    ///     // Resolve the .NET type for the stored event
    ///     Type eventType = eventEntity.ToDomainEventType();
    ///     
    ///     // Use resolved type for deserialization
    ///     var domainEvent = (IDomainEvent)JsonConvert.DeserializeObject(
    ///         eventEntity.Data, 
    ///         eventType);
    ///     
    ///     // Type-specific processing
    ///     if (eventType == typeof(OrderPlacedEvent))
    ///     {
    ///         var orderEvent = (OrderPlacedEvent)domainEvent;
    ///         await ProcessOrderPlacedEvent(orderEvent);
    ///     }
    /// }
    /// catch (InvalidOperationException ex)
    /// {
    ///     _logger.LogError("Domain event type not registered: {TypeName}v{Version}", 
    ///         eventEntity.TypeName, eventEntity.TypeVersion);
    ///     throw;
    /// }
    /// 
    /// // Batch type resolution for multiple entities
    /// public Dictionary&lt;Type, List&lt;EventEntity&gt;&gt; GroupEventsByType(
    ///     List&lt;EventEntity&gt; eventEntities)
    /// {
    ///     var result = new Dictionary&lt;Type, List&lt;EventEntity&gt;&gt;();
    ///     
    ///     foreach (var entity in eventEntities)
    ///     {
    ///         try
    ///         {
    ///             var eventType = entity.ToDomainEventType();
    ///             
    ///             if (!result.ContainsKey(eventType))
    ///                 result[eventType] = new List&lt;EventEntity&gt;();
    ///                 
    ///             result[eventType].Add(entity);
    ///         }
    ///         catch (InvalidOperationException ex)
    ///         {
    ///             _logger.LogWarning("Skipping entity with unregistered type: {TypeName}v{Version}",
    ///                 entity.TypeName, entity.TypeVersion);
    ///         }
    ///     }
    ///     
    ///     return result;
    /// }
    /// 
    /// // Type validation during entity processing
    /// public bool IsValidEventEntity(EventEntity entity)
    /// {
    ///     try
    ///     {
    ///         var eventType = entity.ToDomainEventType();
    ///         return typeof(IDomainEvent).IsAssignableFrom(eventType);
    ///     }
    ///     catch (InvalidOperationException)
    ///     {
    ///         return false;
    ///     }
    /// }
    /// 
    /// // Generic processing with type resolution
    /// public async Task ProcessEventEntity&lt;T&gt;(T entity) where T : IBindableEntity
    /// {
    ///     var eventType = entity.ToDomainEventType();
    ///     var processorType = typeof(IEventProcessor&lt;&gt;).MakeGenericType(eventType);
    ///     var processor = _serviceProvider.GetService(processorType);
    ///     
    ///     if (processor != null)
    ///     {
    ///         var processMethod = processorType.GetMethod("ProcessAsync");
    ///         await (Task)processMethod.Invoke(processor, new[] { entity });
    ///     }
    /// }
    /// </code>
    /// </example>
    public static Type ToDomainEventType(this IBindableEntity bindableEntity)
    {
        var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(bindableEntity.GetTypeBindingKey(), out var eventType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {bindableEntity.TypeName} not found in TypeBindings");
        }

        return eventType!;
    }
}
