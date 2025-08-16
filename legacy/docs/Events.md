# Events

OpenCQRS automatically publishes events unless the feature is disabled (global or command). Events can also be published directly using the dispatcher. Either way there are a few steps to follow in order to handle the events.

First, create an event that inherits from the **Event** class:

```C#
public class SomethingHappened : Event
{
}
```

Next, create one or more handlers:

```C#
public class SomethingHappenedHandlerOne : IEventHandlerAsync<SomethingHappened>
{
    private readonly IServiceOne _serviceOne;

    public SomethingHappenedHandlerOne(IServiceOne serviceOne)
    {
        _serviceOne = serviceOne;
    }

    public Task HandleAsync(SomethingHappened @event)
    {
        return _serviceOne.DoSomethingElseAsync();
    }
}

public class SomethingHappenedHandlerTwo : IEventHandlerAsync<SomethingHappened>
{
    private readonly IServiceTwo _serviceTwo;

    public SomethingHappenedHandlerTwo(IServiceTwo serviceTwo)
    {
        _serviceTwo = serviceTwo;
    }

    public Task HandleAsync(SomethingHappened @event)
    {
        return _serviceTwo.DoSomethingElseAsync();
    }
}
```

As already mentioned, events can be published automatically after a command has been executed (which is the default behaviour) or published directly using the dispatcher:

```C#
var @event = new SomethingHappened();
await _dispatcher.PublishAsync(@event)
```
