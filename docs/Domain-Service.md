# Domain Service

The `IDomainService` interface provides a high-level API for managing aggregates and domain events in an event-sourced system. It abstracts the complexities of event storage, retrieval, and aggregate reconstruction, allowing developers to focus on business logic.

Every store provider has its own implementation of the `IDomainService` interface. You can use it by injecting the interface into your handlers, services, or controllers.

## Available Methods

- [Save Aggregate](#save-aggregate)
- [Save Domain Events](#save-domain-events)
- [Update Aggregate](#update-aggregate)
- [Get Aggregate](#get-aggregate)
- [Get In-Memory Aggregate](#get-in-memory-aggregate)
- [Get Domain Events](#get-domain-events)
- [Get Domain Events From Sequence](#get-domain-events-from-sequence)
- [Get Domain Events Up To Sequence](#get-domain-events-up-to-sequence)
- [Get Domain Events Applied To Aggregate](#get-domain-events-applied-to-aggregate)
- [Get Latest Event Sequence](#get-latest-event-sequence)

<a name="save-aggregate"></a>
### Save Aggregate
Saves an aggregate to the event store with optimistic concurrency control, persisting all uncommitted domain events and updating the aggregate snapshot.

**New aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
```

**Update existing aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var aggregateResult = await domainService.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Error;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);
```

<a name="save-domain-events"></a>
### Save Domain Events
Saves an array of domain events to the event store with optimistic concurrency control, bypassing aggregate persistence. This method is ideal for scenarios where events are generated outside traditional aggregate workflows.
```C#
var streamId = new CustomerStreamId(customerId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var domainEvents = new DomainEvent[]
{
    new OrderPlaced
    {
        OrderId = orderId,
        Amount = 25.45m
    },
    new OrderShipped
    {
        OrderId = orderId,
        ShippedDate = _timeProvider.GetUtcNow()
    }
};
var saveDomainEventsResult = await domainService.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: latestEventSequence);
```

<a name="update-aggregate"></a>
### Update Aggregate
Updates an existing aggregate with new events from its stream, applying any events that occurred after the aggregate's last known state.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var updateAggregateResult = await domainService.UpdateAggregate(streamId, aggregateId);
```

<a name="get-aggregate"></a>
### Get Aggregate
Retrieves an aggregate from the event store, either from its snapshot or by reconstructing it from events.

If the aggregate does not exist, but domain events that can be applied to the aggregate exist, the aggregate snapshot is stored automatically. This is useful when the domain changes, and you need a different aggregate structure. Increase the version of the aggregate type to force a snapshot creation.

```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregateResult = await domainService.GetAggregate(streamId, aggregateId);
```

Optionally, it can be forced to apply any new domain events that occurred after the snapshot was created. This is useful when you want to ensure the aggregate is up to date with the latest events. If new events are found, the aggregate snapshot is updated automatically.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregateResult = await domainService.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
```

<a name="get-in-memory-aggregate"></a>
### Get In-Memory Aggregate
Reconstructs an aggregate entirely from events without using snapshots, providing a pure event-sourced view of the aggregate state.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregateResult = await domainService.GetInMemoryAggregate(streamId, aggregateId);
```

<a name="get-domain-events"></a>
### Get Domain Events
Retrieves all domain events from a specified stream, with optional filtering by event types.
```C#
var streamId = new CustomerStreamId(customerId);
var domainEventsResult = await domainService.GetDomainEvents(streamId);
```
Optionally, you can filter the events by specific event types.
```C#
var streamId = new CustomerStreamId(customerId);
var eventTypes = new Type[] { typeof(OrderPlaced), typeof(OrderShipped) };
var domainEventsResult = await domainService.GetDomainEvents(streamId, eventTypes);
```

<a name="get-domain-events-from-sequence"></a>
### Get Domain Events From Sequence
Retrieves domain events from a specified stream starting from a specific sequence number onwards, with optional filtering by event types.
```C#
var streamId = new CustomerStreamId(customerId);
var fromSequence = 5;
var domainEventsResult = await domainService.GetDomainEventsFromSequence(streamId, fromSequence);
```
Optionally, you can filter the events by specific event types.
```C#
var streamId = new CustomerStreamId(customerId);
var fromSequence = 5;
var eventTypes = new Type[] { typeof(OrderPlaced), typeof(OrderShipped) };
var domainEventsResult = await domainService.GetDomainEventsFromSequence(streamId, fromSequence, eventTypes);
```

<a name="get-domain-events-up-to-sequence"></a>
### Get Domain Events Up To Sequence
Retrieves domain events from a specified stream up to and including a specific sequence number, with optional filtering by event types.
```C#
var streamId = new CustomerStreamId(customerId);
var upToSequence = 10;
var domainEventsResult = await domainService.GetDomainEventsUpToSequence(streamId, upToSequence);
```
Optionally, you can filter the events by specific event types.
```C#
var streamId = new CustomerStreamId(customerId);
var upToSequence = 10;
var eventTypes = new Type[] { typeof(OrderPlaced), typeof(OrderShipped) };
var domainEventsResult = await domainService.GetDomainEventsUpToSequence(streamId, upToSequence, eventTypes);
```

<a name="get-domain-events-applied-to-aggregate"></a>
### Get Domain Events Applied To Aggregate
Retrieves all domain events that have been applied to a specific aggregate instance, using the explicit aggregate-event relationship tracking. This method provides precise access to the events that actually contributed to an aggregate's current state.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var domainEventsResult = await domainService.GetDomainEventsAppliedToAggregate(streamId, aggregateId);
```

<a name="get-latest-event-sequence"></a>
### Get Latest Event Sequence
Retrieves the latest event sequence number for a specified stream, with optional filtering by event types. This method provides the current position in an event stream, essential for optimistic concurrency control and determining where to append new events in event sourcing operations.
```C#
var streamId = new CustomerStreamId(customerId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);
```
Optionally, you can filter the events by specific event types.
```C#
var streamId = new CustomerStreamId(customerId);
var eventTypes = new Type[] { typeof(OrderPlaced), typeof(OrderShipped) };
var latestEventSequence = await domainService.GetLatestEventSequence(streamId, eventTypes);
```
