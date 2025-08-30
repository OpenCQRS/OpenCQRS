using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace OpenCqrs.EventSourcing.Data;

/// <summary>
/// Represents the database entity for storing domain events in an Entity Framework Core event store.
/// This entity captures the complete information needed to persist, retrieve, and reconstruct domain events
/// including their data, metadata, and audit trail.
/// </summary>
/// <example>
/// <code>
/// // Entity Framework configuration
/// public void Configure(EntityTypeBuilder&lt;EventEntity&gt; builder)
/// {
///     builder.HasKey(e =&gt; e.Id);
///     builder.HasIndex(e =&gt; new { e.StreamId, e.Sequence }).IsUnique();
///     builder.Property(e =&gt; e.Data).HasMaxLength(int.MaxValue);
///     builder.Property(e =&gt; e.CreatedDate).IsRequired();
/// }
/// 
/// // Usage in event store
/// public async Task SaveEventsAsync(string streamId, IEnumerable&lt;IDomainEvent&gt; events)
/// {
///     var eventEntities = events.Select((evt, index) =&gt; new EventEntity
///     {
///         Id = Guid.NewGuid().ToString(),
///         StreamId = streamId,
///         Sequence = _nextSequence + index,
///         Data = JsonConvert.SerializeObject(evt),
///         TypeName = evt.DomainEventType().Name,
///         TypeVersion = evt.DomainEventType().Version,
///         CreatedDate = DateTimeOffset.UtcNow,
///         CreatedBy = _currentUser.Id
///     }).ToList();
///     
///     _context.Events.AddRange(eventEntities);
///     await _context.SaveChangesAsync();
/// }
/// 
/// // Loading events from stream
/// public async Task&lt;List&lt;IDomainEvent&gt;&gt; GetEventsAsync(string streamId, int fromSequence = 0)
/// {
///     var eventEntities = await _context.Events
///         .Where(e =&gt; e.StreamId == streamId && e.Sequence &gt;= fromSequence)
///         .OrderBy(e =&gt; e.Sequence)
///         .ToListAsync();
///     
///     return eventEntities.Select(e =&gt; e.ToDomainEvent()).ToList();
/// }
/// </code>
/// </example>
public class EventEntity : IAuditableEntity, IBindableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this domain event instance.
    /// Serves as the primary key and provides global uniqueness across all events in the system.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies this event instance. Typically a GUID or other globally
    /// unique identifier that can be used for event correlation and deduplication.
    /// </value>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the event stream that contains this event.
    /// Links the event to its owning aggregate and enables stream-based event retrieval.
    /// </summary>
    /// <value>
    /// A string that identifies the event stream containing this event. Multiple events
    /// can share the same StreamId, forming an ordered sequence of changes for an aggregate.
    /// </value>
    public string StreamId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sequence number of this event within its stream.
    /// Provides ordering information and supports optimistic concurrency control.
    /// </summary>
    /// <value>
    /// An integer representing the position of this event within its stream. Sequence numbers
    /// start from a base value (typically 0 or 1) and increment for each new event in the stream.
    /// </value>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized representation of the domain event data.
    /// Contains all the business information captured by the domain event.
    /// </summary>
    /// <value>
    /// A JSON string containing the serialized domain event. Includes all properties necessary
    /// to reconstruct the original domain event instance, excluding infrastructure metadata.
    /// </value>
    public string Data { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when this event was created and persisted to the event store.
    /// Provides audit trail information and temporal ordering capabilities.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the event was created and saved.
    /// Should use UTC time for consistency across different time zones and systems.
    /// </value>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that created this event.
    /// Provides audit trail information for security, compliance, and debugging purposes.
    /// </summary>
    /// <value>
    /// A string identifying the user, service, or system that generated and persisted this event.
    /// Can be null if creator information is not available or not required for the use case.
    /// </value>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the logical name of the domain event type as defined in the <see cref="DomainEventType"/> attribute.
    /// Used for type resolution during event deserialization and type binding operations.
    /// </summary>
    /// <value>
    /// A string representing the logical name of the domain event type. Should match the
    /// Name property from the <see cref="DomainEventType"/> attribute on the event class.
    /// </value>
    public string TypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version of the domain event type schema as defined in the <see cref="DomainEventType"/> attribute.
    /// Supports event schema evolution and ensures proper deserialization of different event versions.
    /// </summary>
    /// <value>
    /// An integer representing the schema version of the domain event type. Should match the
    /// Version property from the <see cref="DomainEventType"/> attribute on the event class.
    /// </value>
    public int TypeVersion { get; set; }
}

/// <summary>
/// Provides extension methods for <see cref="EventEntity"/> to support conversion between
/// database entities and domain events in the event sourcing infrastructure.
/// </summary>
public static class EventEntityExtensions
{
    /// <summary>
    /// JSON serializer settings configured specifically for domain event deserialization.
    /// Uses a custom contract resolver to handle private setters and maintain event immutability.
    /// </summary>
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    /// <summary>
    /// Converts an <see cref="EventEntity"/> database entity back into a strongly-typed domain event.
    /// Performs JSON deserialization and type resolution to reconstruct the original domain event.
    /// </summary>
    /// <param name="eventEntity">
    /// The database entity containing the serialized domain event data and type metadata.
    /// </param>
    /// <returns>
    /// A fully reconstructed domain event instance that implements <see cref="IDomainEvent"/>
    /// and matches the original event that was persisted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the event type specified by <see cref="EventEntity.TypeName"/> and 
    /// <see cref="EventEntity.TypeVersion"/> is not found in the <see cref="TypeBindings.DomainEventTypeBindings"/> registry.
    /// This typically indicates that the event type has not been properly registered during application startup.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON data in <see cref="EventEntity.Data"/> cannot be deserialized
    /// to the target event type, usually due to schema incompatibilities or corrupted data.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the deserialized object cannot be cast to <see cref="IDomainEvent"/>,
    /// indicating a type registration or serialization problem.
    /// </exception>
    /// <example>
    /// <code>
    /// // Usage in event store repository
    /// public async Task&lt;List&lt;IDomainEvent&gt;&gt; GetEventsAsync(string streamId)
    /// {
    ///     var eventEntities = await _context.Events
    ///         .Where(e =&gt; e.StreamId == streamId)
    ///         .OrderBy(e =&gt; e.Sequence)
    ///         .ToListAsync();
    ///     
    ///     return eventEntities.Select(e =&gt; e.ToDomainEvent()).ToList();
    /// }
    /// 
    /// // Error handling example
    /// public IDomainEvent ConvertEvent(EventEntity entity)
    /// {
    ///     try
    ///     {
    ///         var domainEvent = entity.ToDomainEvent();
    ///         
    ///         // Verify event was properly reconstructed
    ///         _logger.LogDebug("Converted event: {EventType}", domainEvent.GetType().Name);
    ///         
    ///         return domainEvent;
    ///     }
    ///     catch (InvalidOperationException ex)
    ///     {
    ///         _logger.LogError("Event type not registered: {TypeName}v{Version}", 
    ///             entity.TypeName, entity.TypeVersion);
    ///         throw;
    ///     }
    ///     catch (JsonException ex)
    ///     {
    ///         _logger.LogError("Failed to deserialize event {EventId}: {Error}", 
    ///             entity.Id, ex.Message);
    ///         throw;
    ///     }
    /// }
    /// 
    /// // Bulk conversion with error handling
    /// public List&lt;IDomainEvent&gt; ConvertEvents(List&lt;EventEntity&gt; entities)
    /// {
    ///     var events = new List&lt;IDomainEvent&gt;();
    ///     var errors = new List&lt;string&gt;();
    ///     
    ///     foreach (var entity in entities)
    ///     {
    ///         try
    ///         {
    ///             events.Add(entity.ToDomainEvent());
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             errors.Add($"Event {entity.Id}: {ex.Message}");
    ///         }
    ///     }
    ///     
    ///     if (errors.Any())
    ///     {
    ///         _logger.LogWarning("Failed to convert {Count} events: {Errors}", 
    ///             errors.Count, string.Join("; ", errors));
    ///     }
    ///     
    ///     return events;
    /// }
    /// </code>
    /// </example>
    public static IDomainEvent ToDomainEvent(this EventEntity eventEntity)
    {
        var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(eventEntity.GetTypeBindingKey(), out var eventType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {eventEntity.TypeName} not found in TypeBindings");
        }

        return (IDomainEvent)JsonConvert.DeserializeObject(eventEntity.Data, eventType!, JsonSerializerSettings)!;
    }
}
