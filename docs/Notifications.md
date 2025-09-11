# Notifications

First, create a notification that implements the **INotification** interface:

```C#
public class SomethingHappened : INotification
{
}
```

Next, create one or more handlers:

```C#
public class SomethingHappenedHandlerOne : INotificationHandler<SomethingHappened>
{
    private readonly IServiceOne _serviceOne;

    public SomethingHappenedHandlerOne(IServiceOne serviceOne)
    {
        _serviceOne = serviceOne;
    }

    public Task<Result> Handle(SomethingHappened @notification)
    {
        return _serviceOne.DoSomethingElse();
    }
}

public class SomethingHappenedHandlerTwo : INotificationHandler<SomethingHappened>
{
    private readonly IServiceTwo _serviceTwo;

    public SomethingHappenedHandlerTwo(IServiceTwo serviceTwo)
    {
        _serviceTwo = serviceTwo;
    }

    public Task<Result> Handle(SomethingHappened @notification)
    {
        return _serviceTwo.DoSomethingElse();
    }
}
```

And finally, publish the notification using the dispatcher:

```C#
var notification = new SomethingHappened();
await _dispatcher.Publish(notification)
```
