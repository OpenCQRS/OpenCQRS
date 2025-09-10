using Newtonsoft.Json;

namespace OpenCqrs.EventSourcing.Domain;

/// <summary>
/// Abstract base class for aggregates in event sourcing.
/// </summary>
public abstract class Aggregate : IAggregate
{
    /// <summary>
    /// Gets or sets the stream ID.
    /// </summary>
    [JsonIgnore]
    public string StreamId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the aggregate ID.
    /// </summary>
    [JsonIgnore]
    public string AggregateId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [JsonIgnore]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the latest event sequence.
    /// </summary>
    [JsonIgnore]
    public int LatestEventSequence { get; set; }

    /// <summary>
    /// Private collection of uncommitted events.
    /// </summary>
    [JsonIgnore]
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    /// <summary>
    /// Gets the uncommitted events.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Adds and applies a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void Add(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent);

        if (Apply(domainEvent))
        {
            Version++;
        }
    }

    /// <summary>
    /// Applies a collection of domain events.
    /// </summary>
    /// <param name="domainEvents">The domain events.</param>
    public void Apply(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            if (Apply(domainEvent))
            {
                Version++;
            }
        }
    }

    /// <summary>
    /// Gets the event type filter.
    /// </summary>
    [JsonIgnore]
    public abstract Type[]? EventTypeFilter { get; }

    /// <summary>
    /// Applies a domain event.
    /// </summary>
    /// <typeparam name="TDomainEvent">The domain event type.</typeparam>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>True if applied.</returns>
    protected abstract bool Apply<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;

    /// <summary>
    /// Checks if the domain event type is handled.
    /// </summary>
    /// <param name="domainEventType">The domain event type.</param>
    /// <returns>True if handled.</returns>
    public bool IsDomainEventHandled(Type domainEventType)
    {
        if (EventTypeFilter == null || EventTypeFilter.Length == 0)
        {
            return true;
        }

        return EventTypeFilter.Contains(domainEventType);
    }
}
