using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace OpenCqrs.EventSourcing.Data;

/// <summary>
/// Represents the database entity for storing serialized aggregate snapshots in an Entity Framework Core event store.
/// This entity combines aggregate data with auditing information and type metadata to support event sourcing persistence.
/// </summary>
/// <example>
/// <code>
/// // Entity Framework configuration
/// public void Configure(EntityTypeBuilder&lt;AggregateEntity&gt; builder)
/// {
///     builder.HasKey(e =&gt; e.Id);
///     builder.HasIndex(e =&gt; e.StreamId).IsUnique();
///     builder.Property(e =&gt; e.Data).HasMaxLength(int.MaxValue);
///     
///     // Audit properties
///     builder.Property(e =&gt; e.CreatedDate).IsRequired();
///     builder.Property(e =&gt; e.UpdatedDate).IsRequired();
/// }
/// 
/// // Usage in repository
/// public async Task SaveAggregateAsync&lt;T&gt;(T aggregate) where T : IAggregate
/// {
///     var entity = new AggregateEntity
///     {
///         Id = aggregate.AggregateId,
///         StreamId = aggregate.StreamId,
///         Version = aggregate.Version,
///         LatestEventSequence = aggregate.LatestEventSequence,
///         Data = JsonConvert.SerializeObject(aggregate),
///         TypeName = aggregate.GetTypeBindingKey().Split('_')[0],
///         TypeVersion = int.Parse(aggregate.GetTypeBindingKey().Split('_')[1].Substring(1)),
///         CreatedDate = DateTimeOffset.UtcNow,
///         UpdatedDate = DateTimeOffset.UtcNow,
///         CreatedBy = _currentUser.Id
///     };
///     
///     _context.Aggregates.Add(entity);
///     await _context.SaveChangesAsync();
/// }
/// </code>
/// </example>
public class AggregateEntity : IAuditableEntity, IEditableEntity, IBindableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the aggregate instance.
    /// This serves as the primary key in the database and corresponds to the aggregate's business identifier.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies the aggregate instance. Typically matches the 
    /// <see cref="IAggregate.AggregateId"/> property of the corresponding domain aggregate.
    /// </value>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier for the event stream associated with this aggregate.
    /// Links the aggregate snapshot to its corresponding event stream for consistency verification.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies the event stream containing the aggregate's domain events.
    /// This should match the <see cref="IAggregate.StreamId"/> property of the domain aggregate.
    /// </value>
    public string StreamId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current version of the aggregate based on the number of applied events.
    /// Used for optimistic concurrency control and consistency verification.
    /// </summary>
    /// <value>
    /// An integer representing the aggregate's current version. Increments with each applied domain event
    /// and should match the <see cref="IAggregate.Version"/> property of the domain aggregate.
    /// </value>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the latest event that was applied to create this snapshot.
    /// Provides additional consistency checking beyond the version number.
    /// </summary>
    /// <value>
    /// An integer representing the sequence position of the most recent event applied to this aggregate.
    /// Should match the <see cref="IAggregate.LatestEventSequence"/> property of the domain aggregate.
    /// </value>
    public int LatestEventSequence { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the aggregate's business state.
    /// Contains all the domain data necessary to reconstruct the aggregate instance.
    /// </summary>
    /// <value>
    /// A JSON string containing the serialized aggregate data. This excludes infrastructure
    /// properties and includes only the business state of the aggregate.
    /// </value>
    public string Data { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when this aggregate entity was first created in the database.
    /// Part of the audit trail for tracking aggregate lifecycle.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the aggregate was initially persisted.
    /// Should use UTC time for consistency across time zones.
    /// </value>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that created this aggregate entity.
    /// Provides audit trail information for compliance and debugging purposes.
    /// </summary>
    /// <value>
    /// A string identifying the user, service, or system that initially created the aggregate.
    /// Can be null if the creator information is not available or required.
    /// </value>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this aggregate entity was last modified in the database.
    /// Updated automatically whenever the aggregate snapshot is refreshed.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing the most recent update to the aggregate entity.
    /// Should use UTC time for consistency across time zones.
    /// </value>
    public DateTimeOffset UpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that last modified this aggregate entity.
    /// Maintains audit trail for the most recent changes to the aggregate.
    /// </summary>
    /// <value>
    /// A string identifying the user, service, or system that most recently updated the aggregate.
    /// Can be null if the modifier information is not available or required.
    /// </value>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the logical name of the aggregate type as defined in the <see cref="AggregateType"/> attribute.
    /// Used for type resolution during deserialization and type binding operations.
    /// </summary>
    /// <value>
    /// A string representing the logical name of the aggregate type. This should match the
    /// Name property from the <see cref="AggregateType"/> attribute on the aggregate class.
    /// </value>
    public string TypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version of the aggregate type schema as defined in the <see cref="AggregateType"/> attribute.
    /// Supports aggregate schema evolution and ensures proper deserialization of different versions.
    /// </summary>
    /// <value>
    /// An integer representing the schema version of the aggregate type. This should match the
    /// Version property from the <see cref="AggregateType"/> attribute on the aggregate class.
    /// </value>
    public int TypeVersion { get; set; }
}

/// <summary>
/// Provides extension methods for <see cref="AggregateEntity"/> to support conversion between
/// database entities and domain aggregates in the event sourcing infrastructure.
/// </summary>
public static class AggregateEntityExtensions
{
    /// <summary>
    /// JSON serializer settings configured specifically for aggregate deserialization.
    /// Uses a custom contract resolver to handle private setters and maintain aggregate encapsulation.
    /// </summary>
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    /// <summary>
    /// Converts an <see cref="AggregateEntity"/> database entity back into a strongly-typed domain aggregate.
    /// Performs JSON deserialization, type resolution, and infrastructure property mapping.
    /// </summary>
    /// <typeparam name="T">
    /// The specific aggregate type to deserialize to. Must implement <see cref="IAggregate"/>
    /// and must be registered in the <see cref="TypeBindings.AggregateTypeBindings"/> dictionary.
    /// </typeparam>
    /// <param name="aggregateEntity">
    /// The database entity containing the serialized aggregate data and metadata.
    /// </param>
    /// <returns>
    /// A fully reconstructed domain aggregate instance with both business state (from JSON)
    /// and infrastructure properties (StreamId, Version, etc.) properly populated.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the aggregate type specified by <see cref="AggregateEntity.TypeName"/> and 
    /// <see cref="AggregateEntity.TypeVersion"/> is not found in the <see cref="TypeBindings.AggregateTypeBindings"/> registry.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON data in <see cref="AggregateEntity.Data"/> cannot be deserialized
    /// to the target aggregate type, typically due to schema mismatches or corrupt data.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the deserialized aggregate cannot be cast to the specified generic type parameter.
    /// </exception>
    /// <example>
    /// <code>
    /// // Usage in aggregate repository
    /// public async Task&lt;TAggregate?&gt; GetByIdAsync&lt;TAggregate&gt;(string aggregateId) 
    ///     where TAggregate : class, IAggregate
    /// {
    ///     var entity = await _context.Aggregates
    ///         .FirstOrDefaultAsync(a =&gt; a.Id == aggregateId);
    ///     
    ///     if (entity == null)
    ///         return null;
    ///     
    ///     // Convert entity back to domain aggregate
    ///     return entity.ToAggregate&lt;TAggregate&gt;();
    /// }
    /// 
    /// // Example with error handling
    /// public TAggregate GetAggregate&lt;TAggregate&gt;(AggregateEntity entity) 
    ///     where TAggregate : class, IAggregate
    /// {
    ///     try
    ///     {
    ///         var aggregate = entity.ToAggregate&lt;TAggregate&gt;();
    ///         
    ///         // Verify consistency
    ///         Debug.Assert(aggregate.StreamId == entity.StreamId);
    ///         Debug.Assert(aggregate.Version == entity.Version);
    ///         
    ///         return aggregate;
    ///     }
    ///     catch (InvalidOperationException ex)
    ///     {
    ///         _logger.LogError("Aggregate type not registered: {TypeName}", entity.TypeName);
    ///         throw;
    ///     }
    ///     catch (JsonException ex)
    ///     {
    ///         _logger.LogError("Failed to deserialize aggregate data: {Error}", ex.Message);
    ///         throw;
    ///     }
    /// }
    /// 
    /// // Usage with specific aggregate type
    /// var orderEntity = await _context.Aggregates.FindAsync(orderId);
    /// var orderAggregate = orderEntity.ToAggregate&lt;OrderAggregate&gt;();
    /// 
    /// // Aggregate is now ready for business operations
    /// orderAggregate.ProcessPayment(paymentInfo);
    /// </code>
    /// </example>
    public static T ToAggregate<T>(this AggregateEntity aggregateEntity) where T : IAggregate
    {
        var typeFound = TypeBindings.AggregateTypeBindings.TryGetValue(aggregateEntity.GetTypeBindingKey(), out var aggregateType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Aggregate type {aggregateEntity.TypeName} not found in TypeBindings");
        }

        var aggregate = (T)JsonConvert.DeserializeObject(aggregateEntity.Data, aggregateType!, JsonSerializerSettings)!;
        aggregate.StreamId = aggregateEntity.StreamId;
        aggregate.AggregateId = aggregateEntity.Id;
        aggregate.Version = aggregateEntity.Version;
        aggregate.LatestEventSequence = aggregateEntity.LatestEventSequence;
        return aggregate;
    }
}
