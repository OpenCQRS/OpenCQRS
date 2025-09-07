using System.Reflection;

namespace OpenCqrs.EventSourcing.Domain;

/// <summary>
/// Defines the contract for aggregates in the event sourcing domain model.
/// Aggregates are consistency boundaries that encapsulate business logic and maintain invariants
/// through the application of domain events stored in event streams.
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Gets or sets the unique identifier for the event stream associated with this aggregate.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies the event stream containing this aggregate's domain events.
    /// This is typically derived from the aggregate's identifier and type information.
    /// </value>
    string StreamId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this aggregate instance.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies this specific aggregate instance within its type.
    /// This serves as the primary key for the aggregate and should remain constant throughout its lifetime.
    /// </value>
    string AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the current version of the aggregate based on the number of events applied.
    /// </summary>
    /// <value>
    /// An integer representing the aggregate's version, which increments with each applied domain event.
    /// Used for optimistic concurrency control and tracking aggregate evolution.
    /// </value>
    int Version { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the latest event applied to this aggregate.
    /// </summary>
    /// <value>
    /// An integer representing the sequence position of the most recent event in the event stream.
    /// Used for event ordering and ensuring proper event application sequence.
    /// </value>
    int LatestEventSequence { get; set; }

    /// <summary>
    /// Gets the collection of domain events that have been generated but not yet persisted to the event store.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="IDomainEvent"/> instances representing state changes
    /// that occurred during the current operation but haven't been committed to storage.
    /// </value>
    IEnumerable<IDomainEvent> UncommittedEvents { get; }

    /// <summary>
    /// Applies a collection of domain events to rebuild the aggregate's state.
    /// Used during aggregate reconstruction from the event store.
    /// </summary>
    /// <param name="domainEvents">
    /// The collection of domain events to apply to the aggregate in chronological order.
    /// </param>
    void Apply(IEnumerable<IDomainEvent> domainEvents);

    /// <summary>
    /// Gets an array of event types that this aggregate can handle.
    /// Returns null or empty array if all event types are handled.
    /// </summary>
    /// <value>
    /// An array of <see cref="Type"/> objects representing the domain event types that this aggregate
    /// can process, or null/empty if the aggregate handles all event types.
    /// </value>
    Type[]? EventTypeFilter { get; }

    /// <summary>
    /// Determines whether this aggregate can handle the specified domain event type.
    /// </summary>
    /// <param name="domainEventType">The type of domain event to check.</param>
    /// <returns>
    /// <c>true</c> if the aggregate can handle the specified event type; otherwise, <c>false</c>.
    /// </returns>
    bool IsDomainEventHandled(Type domainEventType);
}

/// <summary>
/// Provides extension methods for <see cref="IAggregate"/> to extract type metadata and support
/// aggregate serialization, versioning, and type binding operations.
/// </summary>
public static class IAggregateExtensions
{
    /// <summary>
    /// Extracts the <see cref="AggregateType"/> attribute information from an aggregate instance.
    /// </summary>
    /// <param name="aggregate">The aggregate instance to extract type information from.</param>
    /// <returns>
    /// The <see cref="AggregateType"/> attribute containing the aggregate's logical name and version.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the aggregate class does not have an <see cref="AggregateType"/> attribute.
    /// </exception>
    public static AggregateType AggregateType(this IAggregate aggregate)
    {
        var aggregateType = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateType == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have an AggregateType attribute.");
        }
        return aggregateType;
    }

    /// <summary>
    /// Generates a type binding key for the aggregate that combines the aggregate name and version
    /// into a format suitable for type resolution and serialization mapping.
    /// </summary>
    /// <param name="aggregate">The aggregate instance to generate a binding key for.</param>
    /// <returns>
    /// A string that uniquely identifies the aggregate type and version combination,
    /// suitable for use in type binding registrations and serialization scenarios.
    /// </returns>
    public static string GetTypeBindingKey(this IAggregate aggregate)
    {
        var aggregateType = aggregate.AggregateType();
        return TypeBindings.GetTypeBindingKey(aggregateType.Name, aggregateType.Version);
    }
}
