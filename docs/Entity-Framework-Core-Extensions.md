# Entity Framework Core Extensions

The Entity Framework Core store provider offers a variety of built-in extension methods of the DbContext to facilitate interaction with aggregates and events. Since the store provider is based purely on the DbContext, it's extremily easy to create your own extensions to create any kind of reporting. Below is a categorized list of the built-in methods:

- [Saving](#saving)
  - [Save Aggregate](#save-aggregate)
  - [Save Domain Events](#save-domain-events)
  - [Save](#save)
  - [Update Aggregate](#update-aggregate)
- [Tracking](#tracking)
  - [Track Aggregate](#track-aggregate)
  - [Track Domain Events](#track-domain-events)
  - [Track Event Entities](#track-event-entities)
- [Retrieving Aggregates and Domain Events](#retrieving-aggregate-and-domain-events)
  - [Get Aggregate](#get-aggregate)
  - [Get In-Memory Aggregate](#get-in-memory-aggregate)
  - [Get Domain Events](#get-domain-events)
  - [Get Domain Events From Sequence](#get-domain-events-from-sequence)
  - [Get Domain Events Up To Sequence](#get-domain-events-up-to-sequence)
  - [Get Domain Events Applied To Aggregate](#get-domain-events-applied-to-aggregate)
  - [Get Latest Event Sequence](#get-latest-event-sequence)
- [Retrieving Database Entities](#retrieving-database-entities)
  - [Get Event Entities](#get-event-entities)
  - [Get Event Entities From Sequence](#get-event-entities-from-sequence)
  - [Get Event Entities Up To Sequence](#get-event-entities-up-to-sequence)
  - [Get Event Entities Applied To Aggregate](#get-event-entities-applied-to-aggregate)
  - [Get Aggregate Event Entities](#get-aggregate-event-entities)

<a name="saving"></a>
## Saving

<a name="save-aggregate"></a>
### Save Aggregate
Saves an aggregate to the event store with optimistic concurrency control, persisting all uncommitted domain events and updating the aggregate snapshot.

**New aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var saveAggregateResult = await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
```

**Update existing aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId);

var aggregateResult = await dbContext.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Error;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

var saveAggregateResult = await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);
```

<a name="save-domain-events"></a>
### Save Domain Events
Saves an array of domain events to the event store with optimistic concurrency control, bypassing aggregate persistence. This method is ideal for scenarios where events are generated outside traditional aggregate workflows.
```C#
var streamId = new CustomerStreamId(customerId);
var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId);

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
var saveDomainEventsResult = await dbContext.SaveDomainEvents(streamId, domainEvents, expectedEventSequence: latestEventSequence);
```

<a name="save"></a>
### Save
Saves all pending changes in the domain database context to the underlying data store. This method provides a simple way to persist tracked entity changes without additional event sourcing logic, suitable for scenarios where entities have been explicitly tracked.
```C#
// ...track aggregates and domain events...

var item = new ItemEntity
{
    Id = Guid.NewGuid(),
    Name = "Sample Item",
    Price = 9.99m
};
dbContext.Items.Add(item);
var saveResult = await dbContext.Save();
```

<a name="update-aggregate"></a>
### Update Aggregate
Updates an existing aggregate with new events from its stream, applying any events that occurred after the aggregate's last known state.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var updateAggregateResult = await dbContext.UpdateAggregate(streamId, aggregateId);
```
<a name="tracking"></a>
## Tracking

<a name="track-aggregate"></a>
### Track Aggregate
Tracks an aggregate's uncommitted events and state changes in the Entity Framework change tracker without persisting to the database, preparing all necessary entities for subsequent save operations.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId);

var aggregateResult = await dbContext.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Error;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

await dbContext.TrackAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);

// ...additional entity changes...

var saveResult = await dbContext.Save();
```

<a name="track-domain-events"></a>
### Track Domain Events
Tracks an array of domain events in the Entity Framework change tracker without persisting to the database, preparing event entities for later save operations with proper sequencing and concurrency control validation.
```C#
var streamId = new CustomerStreamId(customerId);
var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId);

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
await dbContext.TrackDomainEvents(streamId, domainEvents, expectedEventSequence: latestEventSequence);

// ...additional entity changes...

var saveResult = await dbContext.Save();
```

<a name="track-event-entities"></a>
### Track Event Entities
Tracks an aggregate's state changes based on a list of event entities, applying only events that the aggregate can handle and updating its snapshot accordingly.
```C#
var streamId = new CustomerStreamId(customerId);
var orderAggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var trackAggregateResult = await dbContext.TrackAggregate(streamId, orderAggregateId, aggregate, expectedEventSequence: 0);
if (!trackAggregateResult.IsSuccess)
{
    return trackResult.Error;
}
// Track same event entities for a different aggregate
await dbContext.TrackEventEntities(streamId, anotherAggregateId, trackAggregateResult.Value.EventEntities!, expectedEventSequence: 0);

var saveResult = await dbContext.Save();
```
<a name="retrieving-aggregates-and-domain-events"></a>
## Retrieving Aggregates and Domain Events

<a name="get-aggregate"></a>
### Get Aggregate
Retrieves an aggregate from the event store, either from its snapshot or by reconstructing it from events.

If the aggregate does not exist, but domain events that can be applied to the aggregate exist, the aggregate snapshot is stored automatically. This is useful when the domain changes, and you need a different aggregate structure. Increase the version of the aggregate type to force a snapshot creation.

```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregateResult = await dbContext.GetAggregate(streamId, aggregateId);
```

Optionally, it can be forced to apply any new domain events that occurred after the snapshot was created. This is useful when you want to ensure the aggregate is up to date with the latest events. If new events are found, the aggregate snapshot is updated automatically.
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregateResult = await dbContext.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
```

| Method                                | Description                                                                                                                                                                                                                                                                                    |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **GetAggregate**                      | Retrieves an aggregate from the event store, either from its snapshot or by reconstructing it from events.                                                                                                                                                                                     |
| **GetInMemoryAggregate**              | Reconstructs an aggregate entirely from events without using snapshots, providing a pure event-sourced view of the aggregate state.                                                                                                                                                            |
| **GetDomainEvents**                   | Retrieves all domain events from a specified stream, with optional filtering by event types.                                                                                                                                                                                                   |
| **GetDomainEventsFromSequence**       | Retrieves domain events from a specified stream starting from a specific sequence number onwards, with optional filtering by event types.                                                                                                                                                      |
| **GetDomainEventsUpToSequence**       | Retrieves domain events from a specified stream up to and including a specific sequence number, with optional filtering by event types.                                                                                                                                                        |
| **GetDomainEventsAppliedToAggregate** | Retrieves all domain events that have been applied to a specific aggregate instance, using the explicit aggregate-event relationship tracking. This method provides precise access to the events that actually contributed to an aggregate's current state.                                    |
| **GetLatestEventSequence**            | Retrieves the latest event sequence number for a specified stream, with optional filtering by event types. This method provides the current position in an event stream, essential for optimistic concurrency control and determining where to append new events in event sourcing operations. |

## Retrieving Database Entities

| Method                                 | Description                                                                                                                                                                                                        |
|----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **GetEventEntities**                   | Retrieves all event entities from a specified stream, with optional filtering by event types.                                                                                                                      |
| **GetEventEntitiesFromSequence**       | Retrieves a list of event entities from the specified stream starting from a given sequence number, with optional filtering by event types.                                                                        |
| **GetEventEntitiesUpToSequence**       | Retrieves event entities from a specified stream up to and including a specific sequence number, with optional filtering by event types.                                                                           |
| **GetEventEntitiesAppliedToAggregate** | Retrieves all event entities that have been applied to a specific aggregate instance, providing a complete audit trail of changes that contributed to the aggregate's current state.                               |
| **GetAggregateEventEntities**          | Retrieves all aggregate-event relationship entities associated with a specific aggregate instance, providing complete visibility into the many-to-many relationships between the aggregate and its applied events. |
