namespace OpenCqrs.EventSourcing.Domain;

/// <summary>
/// Defines a contract for aggregate identifiers in the event sourcing domain model.
/// Aggregate identifiers uniquely identify aggregate instances within the domain
/// and serve as the primary key for aggregate persistence and retrieval.
/// </summary>
/// <example>
/// <code>
/// // Simple GUID-based aggregate ID
/// public class OrderId : IAggregateId
/// {
///     public string Id { get; }
///     
///     public OrderId() : this(Guid.NewGuid()) { }
///     
///     public OrderId(Guid id)
///     {
///         Id = id.ToString();
///     }
///     
///     public OrderId(string id)
///     {
///         Id = id ?? throw new ArgumentNullException(nameof(id));
///     }
/// }
/// 
/// // Business-meaningful aggregate ID
/// public class CustomerNumber : IAggregateId
/// {
///     public string Id { get; }
///     
///     public CustomerNumber(string customerNumber)
///     {
///         if (string.IsNullOrWhiteSpace(customerNumber))
///             throw new ArgumentException("Customer number cannot be empty", nameof(customerNumber));
///         
///         Id = customerNumber.ToUpperInvariant();
///     }
/// }
/// 
/// // Usage in aggregates
/// public class Order : Aggregate
/// {
///     public string Id { get; private set; }
///     
///     public Order(OrderId id, CustomerId customerId)
///     {
///         Id = id.Id;
///         // Apply domain events...
///     }
/// }
/// </code>
/// </example>
public interface IAggregateId
{
    /// <summary>
    /// Gets the unique string identifier for the aggregate.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies an aggregate instance within its type.
    /// This value must be unique, immutable, and suitable for persistence and indexing.
    /// </value>
    string Id { get; }
}

/// <summary>
/// Defines a strongly-typed contract for aggregate identifiers that are specific to a particular aggregate type.
/// This generic interface provides compile-time type safety and helps prevent mixing IDs between different aggregate types.
/// </summary>
/// <typeparam name="TAggregate">
/// The type of aggregate that this identifier belongs to. Must implement <see cref="IAggregate"/>.
/// This constraint ensures that the ID can only be used with its corresponding aggregate type.
/// </typeparam>
/// <example>
/// <code>
/// // Strongly-typed aggregate ID
/// public class OrderId : IAggregateId&lt;Order&gt;
/// {
///     public string Id { get; }
///     
///     public OrderId(Guid id) =&gt; Id = id.ToString();
/// }
/// 
/// public class CustomerId : IAggregateId&lt;Customer&gt;
/// {
///     public string Id { get; }
///     
///     public CustomerId(string id) =&gt; Id = id;
/// }
/// 
/// // Usage - compile-time type safety
/// public class OrderService
/// {
///     public async Task&lt;Order&gt; GetOrderAsync(OrderId orderId) // Only accepts OrderId
///     {
///         return await repository.GetByIdAsync(orderId);
///     }
/// }
/// 
/// // This would cause a compile error:
/// // var customer = orderService.GetOrderAsync(new CustomerId("123"));
/// </code>
/// </example>
public interface IAggregateId<TAggregate> : IAggregateId where TAggregate : IAggregate;

/// <summary>
/// Provides extension methods for <see cref="IAggregateId"/> to support advanced aggregate identification scenarios
/// including versioning and type-aware operations.
/// </summary>
public static class IAggregateIdExtensions
{
    /// <summary>
    /// Combines the aggregate ID with an aggregate type version to create a versioned identifier.
    /// This is useful for scenarios where different versions of the same aggregate type need distinct identification.
    /// </summary>
    /// <param name="aggregateId">The base aggregate identifier.</param>
    /// <param name="aggregateTypeVersion">The version number of the aggregate type.</param>
    /// <returns>
    /// A string that combines the aggregate ID with the type version in the format "id:version".
    /// </returns>
    /// <example>
    /// <code>
    /// var orderId = new OrderId(Guid.NewGuid());
    /// 
    /// // Create versioned identifiers
    /// var v1Id = orderId.ToIdWithTypeVersion(1); // "guid:1"
    /// var v2Id = orderId.ToIdWithTypeVersion(2); // "guid:2"
    /// 
    /// // Usage in event store queries
    /// var v1Events = await eventStore.GetEventsAsync(v1Id);
    /// var v2Events = await eventStore.GetEventsAsync(v2Id);
    /// 
    /// // Migration scenario
    /// public async Task MigrateAggregateAsync(OrderId orderId)
    /// {
    ///     var v1Stream = orderId.ToIdWithTypeVersion(1);
    ///     var v2Stream = orderId.ToIdWithTypeVersion(2);
    ///     
    ///     var oldEvents = await eventStore.GetEventsAsync(v1Stream);
    ///     var migratedEvents = oldEvents.Select(e =&gt; MigrateEvent(e));
    ///     
    ///     await eventStore.SaveEventsAsync(v2Stream, migratedEvents);
    /// }
    /// </code>
    /// </example>
    public static string ToIdWithTypeVersion(this IAggregateId aggregateId, byte aggregateTypeVersion) =>
        $"{aggregateId.Id}:{aggregateTypeVersion}";
}
