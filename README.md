# OpenCQRS

[![.Build](https://github.com/OpenCQRS/OpenCQRS/actions/workflows/build.yml/badge.svg)](https://github.com/OpenCQRS/OpenCQRS/actions/workflows/build.yml)

.NET framework implementing DDD, Event Sourcing, and CQRS. OpenCQRS 7 is a revamped version of the project with a complete rewrite of the codebase. 

OpenCQRS 7 is extremely flexible and expandable. It can be used as a simple mediator or as a full Event Sourcing solution with Entity Framework Core with any relational database providers (work for a separate Cosmos DB provider is underway).

Missing features from OpenCQRS 6.x will be added in due course.

_Note: OpenCQRS was made private when it had 681 stars and made public again in preparation of version 7._

## Packages

| Package                                                                                                                                               | Beta 3                                                                                                                                                          |
|-------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [OpenCqrs](https://www.nuget.org/packages/OpenCqrs)                                                                                                   | [![Nuget Package](https://img.shields.io/badge/nuget-7.0.0-blue.svg)](https://www.nuget.org/packages/OpenCqrs)                                                  |
| [OpenCqrs.EventSourcing](https://www.nuget.org/packages/OpenCqrs.EventSourcing)                                                                       | [![Nuget Package](https://img.shields.io/badge/nuget-7.0.0-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing)                                    |
| [OpenCqrs.EventSourcing.Store.EntityFrameworkCore](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore)                   | [![Nuget Package](https://img.shields.io/badge/nuget-7.0.0-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore)          |
| [OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity) | [![Nuget Package](https://img.shields.io/badge/nuget-7.0.0-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity) |

## Simple mediator

Three kinds of requests can be sent through the dispatcher:

### Commands

```C#
public class DoSomething : ICommand
{
}

public class DoSomethingHandler : ICommandHandler<DoSomething>
{
    private readonly IMyService _myService;

    public DoSomethingHandler(IMyService myService)
    {
        _myService = myService;
    }

    public async Task<Result> Handle(DoSomething command)
    {
        await _myService.MyMethod();

        return Result.Ok();
    }
}

await _dispatcher.Send(new DoSomething());
```

### Queries

```C#
public class Something
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GetSomething : IQuery<Something>
{
    public int Id { get; set; }
}

public class GetSomethingQueryHandler : IQueryHandler<GetSomething, Something>
{
    private readonly MyDbContext _dbContext;

    public GetProductsHandler(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
        
    public Task<Result<Something>> Handle(GetSomething query)
    {
        return _dbContext.Somethings.FirstOrDefaultAsync(s => s.Id == query.Id);
    }
}

var something = await _dispatcher.Get(new GetSomething { Id = 123 });
```

### Notifications

```C#
public class SomethingHappened : INotifcation
{
}

public class SomethingHappenedHandlerOne : INotifcationHandler<SomethingHappened>
{
    private readonly IServiceOne _serviceOne;

    public SomethingHappenedHandlerOne(IServiceOne serviceOne)
    {
        _serviceOne = serviceOne;
    }

    public Task<Result> Handle(SomethingHappened notification)
    {
        return _serviceOne.DoSomethingElse();
    }
}

public class SomethingHappenedHandlerTwo : INotifcationHandler<SomethingHappened>
{
    private readonly IServiceTwo _serviceTwo;

    public SomethingHappenedHandlerTwo(IServiceTwo serviceTwo)
    {
        _serviceTwo = serviceTwo;
    }

    public Task<Result> Handle(SomethingHappened notification)
    {
        return _serviceTwo.DoSomethingElse();
    }
}

await _dispatcher.Publish(new SomethingHappened());
```

## Event Sourcing with Entity Framework Core

All features are implemented as extension methods on the DbContext, allowing seamless integration with your existing DbContext implementations. IdentityDbContext from ASP.NET Core Identity is also supported.

```C#
[AggregateType("Order")]
puclic class OrderAggregate : Aggregate
{
    public override Type[] EventTypeFilter { get; } =
    [
        typeof(OrderPlaced) // Only events of this type will be loaded for the aggregate from the event stream
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
        };);
    }

    protected override bool Apply<TDomainEvent>(TDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderPlaced @event => Apply(@event)
            _ => false
        };
    }

    private bool Apply(OrderPlaced @event)
    {
        OrderId = @event.OrderId;
        Amount = @event.Amount;

        return true;
    }
}

var streamId = new CustomerStreamId(customerId);
var aggregateId = new OrderAggregateId(orderId);
var aggregate = new OrderAggregate(orderId, amount: 25.45m);

// The save aggregate extension method stores the new events and the snapshot of the aggregate to the latest state
var result = await dbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
```

## Event Sourcing with Cosmos DB

_Work in progress_

## Full Documentation

- [Installation](docs/Installation.md)
- [Configuration](docs/Configuration.md)
- [Basics](docs/Basics.md)
  - [Commands](docs/Commands.md)
  - [Events](docs/Events.md)
  - [Queries](docs/Queries.md)
- [Event Sourcing](docs/Event-Sourcing.md)
  - [Domain](docs/Domain.md)
  - [Store Providers](docs/Store-Providers.md)
    - [Entity Framework Core](docs/Entity-Framework-Core.md)
      - [Extensions](docs/Entity-Framework-Core-Extensions.md)
      - [Scenarios](docs/Entity-Framework-Core-Scenarios.md)
    - [Cosmos DB](docs/Cosmos.md)
- [Release Notes](docs/Release-Notes.md)

_[Legacy documentation here (OpenCQRS 6.x)](docs-6.x/index.md)_
