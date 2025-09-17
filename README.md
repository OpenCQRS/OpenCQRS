# 🚀 OpenCQRS&trade;

[![.Build](https://github.com/OpenCQRS/OpenCQRS/actions/workflows/build.yml/badge.svg)](https://github.com/OpenCQRS/OpenCQRS/actions/workflows/build.yml)

.NET framework implementing DDD, Event Sourcing, and CQRS.

OpenCQRS 7 released in September 2025 is extremely flexible and expandable. It can be used as a simple mediator or as a full Event Sourcing solution with Cosmos DB or Entity Framework Core as storage.

- [Full documentation](https://opencqrs.github.io/OpenCQRS/)
- [Examples in repository](#examples)
- [Ecommerce demo application (WIP)](https://github.com/OpenCQRS/EventShop)

## ⭐ Give a star

If you're using this repository for your learning, samples, workshop, or your project, please give a star. Thank you!

## ⚡Main Features

- Mediator with commands, queries, and notifications
- Multiple aggregates per stream
- Option to store the aggregate snapshot alongside events for fast reads, and write model strongly consistent
- In memory aggregate reconstruction up to a specific event sequence or date if provided _**(soon up to aggregate version)**_
- Events applied to the aggregate filtered by event type
- Retrieval of all events applied to an aggregate
- Querying stream events from or up to a specific event sequence or date/date range
- Optimistic concurrency control with an expected event sequence
- Automatic event/notification publication after a command is successfully processed that returns a list of results from all notification handlers
- Automatic event/message publication after a command is successfully processed using Service Bus or RabbitMQ
- Automatic command validation with FluentValidation if required
- Command sequences that return a list of results from all commands in the sequence
- Custom command handlers or services can be used instead of the automatically resolved command handlers
- Result pattern across handlers and providers
- Extensible architecture with providers for store, messaging, caching, and validation

## 🗺️ Roadmap

### ⏳ In Progress
- Create an ecommerce demo application to showcase OpenCQRS features

### ⏭️ Next
- Option to automatically validate a command
- Option to automatically apply new events when getting an aggregate snapshot

### 🕙 To Follow
- Event Grid messaging provider
- Kafka messaging provider
- File store provider for event sourcing
- Amazon SQS messaging provider
- EventSourcingDB store provider

## 📦 Nuget Packages

| Package                                                                                                                                               | Latest Stable                                                                                                                                                   |
|-------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [OpenCqrs](https://www.nuget.org/packages/OpenCqrs)                                                                                                   | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs)                                                  |
| [OpenCqrs.EventSourcing](https://www.nuget.org/packages/OpenCqrs.EventSourcing)                                                                       | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing)                                    |
| [OpenCqrs.EventSourcing.Store.Cosmos](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.Cosmos)                                             | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.Cosmos)                       |
| [OpenCqrs.EventSourcing.Store.EntityFrameworkCore](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore)                   | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore)          |
| [OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity) | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity) |
| [OpenCqrs.Messaging.RabbitMq](https://www.nuget.org/packages/OpenCqrs.Messaging.RabbitMq)                                                             | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.Messaging.RabbitMq)                               |
| [OpenCqrs.Messaging.ServiceBus](https://www.nuget.org/packages/OpenCqrs.Messaging.ServiceBus)                                                         | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.Messaging.ServiceBus)                             |
| [OpenCqrs.Validation.FluentValidation](https://www.nuget.org/packages/OpenCqrs.Validation.FluentValidation)                                           | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.Validation.FluentValidation)                      |
| [OpenCqrs.Caching.Redis](https://www.nuget.org/packages/OpenCqrs.Caching.Redis)                                                                       | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.Caching.Redis)                                    |
| [OpenCqrs.Caching.Memory](https://www.nuget.org/packages/OpenCqrs.Caching.Memory)                                                                     | [![Nuget Package](https://img.shields.io/badge/nuget-7.1.5-blue.svg)](https://www.nuget.org/packages/OpenCqrs.Caching.Memory)                                   |

## 🔄 Simple mediator

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

## 💾 Event Sourcing

You can use the `IDomainService` interface to access the event-sourcing functionalities for every store provider.

In the Cosmos DB store provider you can also use the `ICosmosDataStore` interface to access Cosmos DB specific features.

In the Entity Framework Core store provider you can also use the `IDomainDbContext` extensions to access Entity Framework Core specific features.
In the Entity Framework Core store provider, IdentityDbContext from ASP.NET Core Identity is also supported.

```C#
[AggregateType("Order")]
puclic class Order : AggregateRoot
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
        };);
    }

    protected override bool Apply<T>(T @event)
    {
        return @event switch
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
var aggregateId = new OrderId(orderId);
var aggregate = new Order(orderId, amount: 25.45m);

// Save aggregate stores the uncommitted events and the snapshot of the aggregate
var saveAggregateResult = await domainService.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence: 0);
// the alternative is to store the events and the snapshot separately
var saveEventsResult = await domainService.SaveEvents(streamId, aggregate.UncommittedEvents(), expectedEventSequence: 0);
var updateAggregateResult = await domainService.UpdateAggregate(streamId, aggregateId);
```

<a name="examples"></a>
## 📚 Examples

Examples of how to use OpenCQRS can be found in the repository:

- [OpenCQRS.Examples.Caching.Memory](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.Caching.Memory)
- [OpenCQRS.Examples.Caching.Redis](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.Caching.Redis)
- [OpenCQRS.Examples.EventSourcing.Cosmos](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.EventSourcing.Cosmos)
- [OpenCQRS.Examples.EventSourcing.EntityFrameworkCore](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.EventSourcing.EntityFrameworkCore)
- [OpenCQRS.Examples.Messaging.RabbitMq](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.Messaging.RabbitMq)
- [OpenCQRS.Examples.Messaging.ServiceBus](https://github.com/OpenCQRS/OpenCQRS/tree/main/examples/OpenCqrs.Examples.Messaging.ServiceBus)
