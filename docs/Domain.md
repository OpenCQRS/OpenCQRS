# Domain

- [Stream Id](#stream-id)
- [Domain Events](#domain-events)
- [Aggregate Id](#aggregate-id)
- [Aggregate](#aggregate)

<a name="stream-id"></a>
## Stream Id

A Stream Id is a unique identifier that represents a specific event stream. For example, a stream could represent all events related to a specific customer or order.

```C#
public class CustomerStreamId(string customerId) : IStreamId
{
    public string Id => $"customer:{customerId}";
}

var streamId = new CustomerStreamId(customerId);
```

<a name="domain-events"></a>
## Domain Events

Domain events represent business decisions that have happened in the domain and are stored as part of an event stream.

```C#
[DomainEventType("OrderPlaced")]
public record OrderPlacedEvent(Guid orderId, decimal amount) : IDomainEvent;
```

<a name="aggregate-id"></a>
## Aggregate Id

An Aggregate Id uniquely identifies aggregate instances within the domain and serves as the primary key for aggregate persistence and retrieval.

```C#
public class OrderAggregateId(string orderId) : IAggregateId<OrderAggregate>
{
    public string Id => $"order:{orderId}";
}

var aggregateId = new OrderAggregateId(orderId);
```

<a name="aggregate"></a>
## Aggregate

Aggregates are consistency boundaries that encapsulate business logic and maintain invariants through the application of domain events stored in event streams. 

Domain events in an event stream can be handled by multiple aggregates, but each aggregate instance is responsible for applying only the events that pertain to it.

Aggregates have an event type filter that specifies which types of events they can handle. When loading an aggregate from an event stream, only the events that match the aggregate's event type filter are applied to reconstruct its state. If no events are specified in the filter, the aggregate will load all events from the stream.

```C#
[AggregateType("Order")]
public class Order : Aggregate
{
    public override Type[] EventTypeFilter { get; } =
    [
        typeof(OrderPlaced)
    ];
        
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }

    public Order() { }

    public Order(Guid orderId, decimal amount)
    {
        Add(new OrderPlaced
        {
            OrderId = orderId,
            Amount = amount
        });
    }

    protected override bool Apply<TDomainEvent>(TDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderPlaced @event => Apply(@event),
            _ => false
        };
    }

    private bool Apply(OrderPlaced @event)
    {
        OrderId = @event.OrderId;
        Amount = @event.amount;

        return true;
    }
}
```
