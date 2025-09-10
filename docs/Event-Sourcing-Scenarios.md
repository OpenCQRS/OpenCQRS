# Event Sourcing Scenarios

Some common scenarios when using Event Sourcing.

- [Events handled by a single aggregate](#1)
  - [New aggregate](#1.1)
  - [Existing aggregate](#1.2)
- [Events handled by multiple aggregates](#2)
  - [New aggregate](#2.1)
  - [Existing aggregate](#2.2)

<a name="1"></a>
## Events handled by a single aggregate

<a name="1.1"></a>
### New aggregate

Save aggregate method stores the new events and the snapshot of the aggregate to the latest state
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
```

or save domain events and aggregate snapshot separately
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var events = new @event[]
{
    new OrderPlaced
    {
        OrderId = orderId,
        Amount = 25.45m
    }
};
var saveDomainsEventResult = await domainService.SaveEvents(streamId, events, expectedEventSequence: 0);

// Get aggregate creates a new aggregate instance and applies the events from the stream to it,
// and stores the snapshot of the aggregate to the latest state
var aggregate = await domainService.GetAggregate(streamId, aggregateId);
```

<a name="1.2"></a>
### Existing aggregate

Save aggregate method stores the new events and the snapshot of the aggregate to the latest state
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var aggregateResult = await domainService.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Failure;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);
```

or save domain events and aggregate snapshot separately
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var events = new @event[]
{
    new AmountUpdated
    {
        OrderId = orderId,
        Amount = 15.00m
    }
};
var saveDomainsEventResult = await domainService.SaveEvents(streamId, events, expectedEventSequence: latestEventSequence);

// The new event stored separately is applied to the aggregate when retrieved,
// and the snapshot of the aggregate is stored to the latest state
var aggregateResult = await domainService.GetAggregate(streamId, aggregateId, applyNewEvents: true);
```

<a name="2"></a>
## Events handled by multiple aggregates

<a name="2.1"></a>
### New aggregates

Save the first aggregate with new event(s) and the snapshot of the other aggregate to the latest state
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

// The event stored initially for another the aggregate is applied to the other aggregate when
// retrieved (assuming the event type is handled by the other aggregate),
// and the snapshot of the other aggregate is stored to the latest state
var aggregateResult = await domainService.GetAggregate(streamId, anotherAggregateId);
```

or save domain events and aggregate snapshots separately
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var events = new @event[]
{
    new OrderPlaced
    {
        OrderId = orderId,
        Amount = 25.45m
    }
};
var saveDomainsEventResult = await domainService.SaveEvents(streamId, events, expectedEventSequence: 0);

// Get aggregate creates a new aggregate instance and applies the events from the stream to it,
// and stores the snapshot of the aggregate to the latest state
var aggregate = await domainService.GetAggregate(streamId, aggregateId);
var anotherAggregate = await domainService.GetAggregate(streamId, anotherAggregateId);
```

<a name="2.2"></a>
### Existing aggregates

Save the first aggregate with new event(s) and the snapshot of the other aggregate to the latest state
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var aggregateResult = await domainService.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Failure;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);
var anotherAggregateResult = await domainService.GetAggregate(streamId, anotherAggregateId, applyNewEvents: true);
```

or save domain events and aggregate snapshots separately
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var latestEventSequence = await domainService.GetLatestEventSequence(streamId);

var events = new @event[]
{
    new AmountUpdated
    {
        OrderId = orderId,
        Amount = 15.00m
    }
};
var saveDomainsEventResult = await domainService.SaveEvents(streamId, events, expectedEventSequence: latestEventSequence);

// The new event stored separately is applied to the aggregate when retrieved,
// and the snapshot of the aggregate is stored to the latest state
var aggregateResult = await domainService.GetAggregate(streamId, aggregateId, applyNewEvents: true);
var anotherAggregateResult = await domainService.GetAggregate(streamId, anotherAggregateId, applyNewEvents: true);
```
