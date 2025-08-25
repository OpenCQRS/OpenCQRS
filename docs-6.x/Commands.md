# Commands

The dispatcher will automatically publish any events returned by the handler unless the property **PublishEvents** is set to _false_ on a global or a command level (see [Configuration](Configuration#main)).

First, create a command that inherits from the **Command** class:

```C#
public class DoSomething : Command
{
}
```

Second, create an event that inherits from the **Event** class:

```C#
public class SomethingHappened : Event
{
}
```

Next, create the handler that implements the default ICommandHandlerAsync<ICommand> interface or create a custom one:

```C#
// Option 1 - Default command handler
public class DoSomethingHandler : ICommandHandlerAsync<DoSomething>
{
    private readonly IMyService _myService;

    public DoSomethingHandler(IMyService myService)
    {
        _myService = myService;
    }

    public async Task<CommandResponse> HandleAsync(DoSomething command)
    {
        await _myService.MyMethodAsync();

        return new CommandResponse
        {
            Events = new List<IDomainEvent>()
            {
                new SomethingHappened()
            }
        };
    }
}

// Option 2 - Custom command handler or service
public interface ISomethingService
{
    Task<CommandResponse> DoSomethingAsync(DoSomething command);
}

public class SomethingService : ISomethingService
{
    private readonly IMyService _myService;

    public SomethingService(IMyService myService)
    {
        _myService = myService;
    }

    public async Task<CommandResponse> DoSomethingAsync(DoSomething command)
    {
        await _myService.MyMethodAsync();

        return new CommandResponse
        {
            Events = new List<IDomainEvent>()
            {
                new SomethingHappened()
            }
        };
    }
}
```

And finally, send the command using the dispatcher:

```C#
var command = new DoSomething();

// Option 1 - The dispatcher will automatically resolve the command handler (ICommandHandlerAsync<DoSomething>)
await _dispatcher.SendAsync(command);

// Option 2 - Use your custom command handler or service
await _dispatcher.SendAsync(command, () => _somethingService.DoSomethingAsync(command));
```

It is also possible to get a result from the command handler by specifying the type in the dispatcher:

```C#
var command = new DoSomething();

// Option 1 - The dispatcher will automatically resolve the command handler (ICommandHandlerAsync<DoSomething>)
await _dispatcher.SendAsync<bool>(command);

// Option 2 - Use your custom command handler or service
await _dispatcher.SendAsync<bool>(command, () => _somethingService.DoSomethingAsync(command));
```

And by setting the value of the result in the command response:

```C#
public class DoSomethingHandler : ICommandHandlerAsync<DoSomething>
{
    private readonly IMyService _myService;

    public DoSomethingHandler(IMyService myService)
    {
        _myService = myService;
    }

    public async Task<CommandResponse> HandleAsync(DoSomething command)
    {
        await _myService.MyMethodAsync();

        return new CommandResponse
        {
            Events = new List<IDomainEvent>()
            {
                new SomethingHappened()
            },
            Result = true
        };
    }
}
```

Note that two optional properties can be set in the commands: **UserId** and **Source**.

## Related

- [Command Validation](Command-Validation)
- [Command Sequence](Command-Sequence)
- [Domain Commands With Event Sourcing](With-Event-Sourcing)
- [Domain Commands Without Event Sourcing](Without-Event-Sourcing)
