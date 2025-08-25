# Events

First, create an event that implements the **IEvent** interface:

```C#
public class SomethingHappened : IEvent
{
}
```

Next, create one or more handlers:

```C#
public class SomethingHappenedHandlerOne : IEventHandler<SomethingHappened>
{
    private readonly IServiceOne _serviceOne;

    public SomethingHappenedHandlerOne(IServiceOne serviceOne)
    {
        _serviceOne = serviceOne;
    }

    public Task<Result> Handle(SomethingHappened @event)
    {
        return _serviceOne.DoSomethingElse();
    }
}

public class SomethingHappenedHandlerTwo : IEventHandler<SomethingHappened>
{
    private readonly IServiceTwo _serviceTwo;

    public SomethingHappenedHandlerTwo(IServiceTwo serviceTwo)
    {
        _serviceTwo = serviceTwo;
    }

    public Task<Result> Handle(SomethingHappened @event)
    {
        return _serviceTwo.DoSomethingElse();
    }
}
```

And finally, publish the event using the dispatcher:

```C#
var @event = new SomethingHappened();
await _dispatcher.Publish(@event)
```
