using Newtonsoft.Json;

namespace OpenCqrs.EventSourcing.Domain;

/// <summary>
/// Abstract base class that provides default implementation for the <see cref="IAggregate"/> interface.
/// Handles event application, versioning, and uncommitted event tracking for event sourcing scenarios.
/// </summary>
/// <example>
/// <code>
/// [AggregateType("Order", 1)]
/// public class OrderAggregate : Aggregate
/// {
///     public Guid OrderId { get; private set; }
///     public Guid CustomerId { get; private set; }
///     public OrderStatus Status { get; private set; }
///     public List&lt;OrderItem&gt; Items { get; private set; } = new();
///     public decimal TotalAmount { get; private set; }
/// 
///     // Required for event sourcing reconstruction
///     public OrderAggregate() { }
/// 
///     // Business constructor
///     public OrderAggregate(Guid orderId, Guid customerId, List&lt;OrderItem&gt; items)
///     {
///         if (orderId == Guid.Empty) throw new ArgumentException("Order ID cannot be empty");
///         if (customerId == Guid.Empty) throw new ArgumentException("Customer ID cannot be empty");
///         if (!items.Any()) throw new ArgumentException("Order must have items");
/// 
///         var orderPlaced = new OrderPlacedEvent
///         {
///             OrderId = orderId,
///             CustomerId = customerId,
///             Items = items,
///             PlacedAt = DateTime.UtcNow
///         };
/// 
///         Add(orderPlaced); // This will apply the event and increment version
///     }
/// 
///     // Business method
///     public void ShipOrder(string trackingNumber)
///     {
///         if (Status != OrderStatus.Confirmed)
///             throw new InvalidOperationException("Only confirmed orders can be shipped");
/// 
///         var orderShipped = new OrderShippedEvent
///         {
///             OrderId = OrderId,
///             TrackingNumber = trackingNumber,
///             ShippedAt = DateTime.UtcNow
///         };
/// 
///         Add(orderShipped);
///     }
/// 
///     // Event type filter - only handle order-related events
///     public override Type[]? EventTypeFilter =&gt; new[]
///     {
///         typeof(OrderPlacedEvent),
///         typeof(OrderConfirmedEvent),
///         typeof(OrderShippedEvent),
///         typeof(OrderCancelledEvent)
///     };
/// 
///     // Event application method
///     protected override bool Apply&lt;TDomainEvent&gt;(TDomainEvent domainEvent)
///     {
///         return domainEvent switch
///         {
///             OrderPlacedEvent e =&gt; Apply(e),
///             OrderConfirmedEvent e =&gt; Apply(e),
///             OrderShippedEvent e =&gt; Apply(e),
///             OrderCancelledEvent e =&gt; Apply(e),
///             _ =&gt; false
///         };
///     }
/// 
///     // Specific event handlers
///     private bool Apply(OrderPlacedEvent @event)
///     {
///         OrderId = @event.OrderId;
///         CustomerId = @event.CustomerId;
///         Items = @event.Items.ToList();
///         Status = OrderStatus.Placed;
///         TotalAmount = Items.Sum(i =&gt; i.Price * i.Quantity);
///         return true;
///     }
/// 
///     private bool Apply(OrderShippedEvent @event)
///     {
///         Status = OrderStatus.Shipped;
///         return true;
///     }
/// }
/// </code>
/// </example>
public abstract class Aggregate : IAggregate
{
    /// <summary>
    /// Gets or sets the unique identifier for the event stream associated with this aggregate.
    /// Marked with [JsonIgnore] as it's infrastructure data handled separately from business state.
    /// </summary>
    [JsonIgnore]
    public string StreamId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier for this aggregate instance.
    /// Marked with [JsonIgnore] as it's infrastructure data handled separately from business state.
    /// </summary>
    [JsonIgnore]
    public string AggregateId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current version of the aggregate.
    /// Marked with [JsonIgnore] as it's infrastructure data managed by the event sourcing framework.
    /// </summary>
    [JsonIgnore]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the latest event applied to this aggregate.
    /// Marked with [JsonIgnore] as it's infrastructure data managed by the event sourcing framework.
    /// </summary>
    [JsonIgnore]
    public int LatestEventSequence { get; set; }

    /// <summary>
    /// Private collection that stores uncommitted domain events generated during the current operation.
    /// </summary>
    [JsonIgnore]
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    /// <summary>
    /// Gets the read-only collection of domain events that have been generated but not yet persisted.
    /// These events represent state changes that occurred during the current operation.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the uncommitted events collection and applies it to update the aggregate state.
    /// This is the primary method for generating events within business operations.
    /// </summary>
    /// <param name="domainEvent">The domain event to add and apply to the aggregate.</param>
    protected void Add(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent);

        if (Apply(domainEvent))
        {
            Version++;
        }
    }

    /// <summary>
    /// Applies a collection of domain events to rebuild the aggregate's state during reconstruction.
    /// Used when loading aggregates from the event store to replay their complete history.
    /// </summary>
    /// <param name="domainEvents">
    /// The collection of historical domain events to apply in chronological order.
    /// </param>
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
    /// Gets an array of event types that this aggregate can handle.
    /// Concrete aggregates should override this property to specify which events they process.
    /// </summary>
    /// <value>
    /// An array of <see cref="Type"/> objects representing the domain event types this aggregate handles,
    /// or null/empty array if all event types should be processed.
    /// </value>
    [JsonIgnore]
    public abstract Type[]? EventTypeFilter { get; }

    /// <summary>
    /// Abstract method that concrete aggregates must implement to handle domain event application.
    /// This method should contain the logic for applying specific event types to update aggregate state.
    /// </summary>
    /// <typeparam name="TDomainEvent">The specific type of domain event being applied.</typeparam>
    /// <param name="domainEvent">The domain event instance to apply to the aggregate.</param>
    /// <returns>
    /// <c>true</c> if the event was successfully applied and the version should be incremented;
    /// <c>false</c> if the event was not handled or should not affect the version.
    /// </returns>
    protected abstract bool Apply<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;

    /// <summary>
    /// Determines whether this aggregate can handle the specified domain event type
    /// based on the configured <see cref="EventTypeFilter"/>.
    /// </summary>
    /// <param name="domainEventType">The type of domain event to check.</param>
    /// <returns>
    /// <c>true</c> if the event type can be handled (either no filter is set or the type is in the filter);
    /// <c>false</c> if the event type is filtered out.
    /// </returns>
    public bool IsDomainEventHandled(Type domainEventType)
    {
        if (EventTypeFilter == null || EventTypeFilter.Length == 0)
        {
            return true;
        }

        return EventTypeFilter.Contains(domainEventType);
    }
}
