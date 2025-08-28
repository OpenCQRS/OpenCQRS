# Commands

- [Simple commands](#simple-commands)
- [Commands with a result](#commands-with-results)
- [Commands that publish notifications](#commands-with-notifications)
- [Commands validation](Command-Validation)

<a name="simple-commands"></a>
## Simple commands

First, create a command that implements the **ICommand** interface:

```C#
public class DoSomething : ICommand
{
}
```

Next, create the handler that implements the ICommandHandler<ICommand> interface:

```C#
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
```

And finally, send the command using the dispatcher:

```C#
var command = new DoSomething();
await _dispatcher.Send(command);
```

<a name="commands-with-results"></a>
## Commands with a result

It is also possible to get a response from a command handler by using the **ICommand&lt;TResponse&gt;** interface:

```C#
public class DoSomethingAndGetResult : ICommand<MyResult>
{
}
```
```C#
public class DoSomethingAndGetResultHandler : ICommandHandler<DoSomethingAndGetResult, MyResult>
{
    private readonly IMyService _myService;
    
    public DoSomethingAndGetResultHandler(IMyService myService)
    {
        _myService = myService;
    }
    
    public async Task<Result<MyResult>> Handle(DoSomethingAndGetResult command)
    {
        var result = await _myService.MyMethodAndGetResult();
        return result;
    }
}
```
```C#
var command = new DoSomethingAndGetResult();
var result = await _dispatcher.Send(command);
```

<a name="commands-with-notifications"></a>
## Commands that publish notifications

If you want to automatically publish notifications on the back of a successfully processed command, you can use the SendAndPublish method:

Define a command that implements the **ICommand&lt;CommandResponse&gt;** interface and a notification that implements the **INotification** interface:
```C#
public record DoSomething(string Name) : ICommand<CommandResponse>;
public record SomethingHappened(string Name) : INotification;
```

Then, create the command handler that implements the **ICommandHandler&lt;ICommand&lt;CommandResponse&gt;, CommandResponse&gt;** interface and the notification handlers that implement the **INotificationHandler&lt;INotification&gt;** interface:
```C#
public class DoSomethingHandler : ICommandHandler<DoSomething, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(DoSomething command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var notification = new SomethingHappened(command.Name);

        var response = new CommandResponse(
            notification,
            new { Message = $"Successfully processed command for: {command.Name}" }
        );

        return response;
    }
}

public class SomethingHappenedHandlerOne : INotificationHandler<SomethingHappened>
{
    public Task<Result> Handle(SomethingHappened notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Handler One processed: {notification.Name}");

        return Task.FromResult(Result.Ok());
    }
}

public class SomethingHappenedHandlerTwo : INotificationHandler<SomethingHappened>
{
    public Task<Result> Handle(SomethingHappened notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Handler Two processed: {notification.Name}");

        return Task.FromResult(Result.Ok());
    }
}
```

Finally, send the command using the dispatcher:
```C#
var command = new DoSomething("MyName");
var result = await _dispatcher.SendAndPublish(command);
```
The result will contain the command result and all results from the notification handlers:
```C#
{
    "CommandResult": {
        "IsSuccess": true,
        "Value": {
            "Message": "Successfully processed command for: MyName"
        },
        "Error": null
    },
    "NotificationResults": [
        {
            "IsSuccess": true,
            "Value": null,
            "Error": null
        },
        {
            "IsSuccess": true,
            "Value": null,
            "Error": null
        }
    ]
}
```

<a name="commands-with-validation"></a>
## Commands validation

You can validate commands automatically before they are sent to the command handler. The validation is made by the validation provider registered (e.g., FluentValidation).

Just use the optional `validateCommand` parameter in any of the dispatcher methods:

```C#
var result = await _dispatcher.Send(command, validateCommand: true);
var result = await _dispatcher.SendAndPublish(command, validateCommand: true);
```
If the command is not valid, the command handler will not be called and the result will contain the validation errors:

```C#
{
    "IsSuccess": false,
    "Value": null,
    "Error": {
        "Message": "Validation failed",
        "Details": [
            "Name must not be empty",
            "Age must be greater than 0"
        ]
    }
}
```
