# Entity Framework Core Scenarios

Some common scenarios when using Event Sourcing with Entity Framework Core.

## 1. Events handled by a single aggregate

### 1.1 Save aggregate with events

**New aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var saveAggregateResult = await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
```

**Existing aggregate**
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

### 1.2 Track aggregate with events + other tracking + save changes

**New aggregate**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

await dbContext.TrackAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);

// ...additional tracking...

var saveResult = await dbContext.Save();
```

**Existing aggregate**
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

// ...additional tracking...

var saveResult = await dbContext.Save();
```

## 2 Events handled by multiple aggregates

### 2.1 Track the main aggregate with events + track additional aggregates + other tracking + save changes

**New stream**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

var trackAggregateResult = await dbContext.TrackAggregate(streamId, orderAggregateId, aggregate, expectedEventSequence: 0);
if (!trackAggregateResult.IsSuccess)
{
    return trackResult.Error;
}

await dbContext.TrackEventEntities(streamId, anotherAggregateId, trackAggregateResult.Value.EventEntities!, expectedEventSequence: 0);

// ...additional tracking...

var saveResult = await dbContext.Save();
```

**Existing stream**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
var latestEventSequence = await domainDbContext.GetLatestEventSequence(streamId);

var aggregateResult = await dbContext.GetAggregate(streamId, aggregateId);
if (!aggregateResult.IsSuccess)
{
    return aggregateResult.Error;
}
aggregate = aggregateResult.Value;

aggregate.UpdateAmount(amount: 15.00m);

var trackAggregateResult = await dbContext.TrackAggregate(streamId, aggregateId, aggregate, expectedEventSequence: latestEventSequence);
if (!trackAggregateResult.IsSuccess)
{
    return trackResult.Error;
}

await dbContext.TrackEventEntities(streamId, anotherAggregateId, trackAggregateResult.Value.EventEntities!, expectedEventSequence: latestEventSequence);

// ...additional tracking...

var saveResult = await dbContext.Save();
```

### 2.2 Track events + track aggregates + other tracking + save changes

**New stream**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);

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
var trackDomainEntitiesResult = await dbContext.TrackDomainEvents(streamId, domainEvents, expectedEventSequence: 0);
if (!trackDomainEntitiesResult.IsSuccess)
{
    return trackDomainEntitiesResult.Error;
}
var eventEntities = trackDomainEntitiesResult.Value.EventEntities!;

await dbContext.TrackEventEntities(streamId, aggregateId, eventEntities, expectedEventSequence: 0);
await dbContext.TrackEventEntities(streamId, anotherAggregateId, eventEntities, expectedEventSequence: 0);

// ...additional entity changes...

var result = await dbContext.Save();
```

**Existing stream**
```C#
var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var anotherAggregateId = new AnotherAggregateId(orderId);
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
var trackDomainEntitiesResult = await dbContext.TrackDomainEvents(streamId, domainEvents, expectedEventSequence: latestEventSequence);
if (!trackDomainEntitiesResult.IsSuccess)
{
    return trackDomainEntitiesResult.Error;
}
var eventEntities = trackDomainEntitiesResult.Value.EventEntities!;

await dbContext.TrackEventEntities(streamId, aggregateId, eventEntities, expectedEventSequence: latestEventSequence);
await dbContext.TrackEventEntities(streamId, anotherAggregateId, eventEntities, expectedEventSequence: latestEventSequence);

// ...additional entity changes...

var result = await dbContext.Save();
```
