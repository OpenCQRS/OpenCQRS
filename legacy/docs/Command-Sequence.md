# Command Sequence

With OpenCQRS is possible to create a squence of commands that will be executed in the specified order.

First, create the commands that need to be part of the sequence:

```C#
public class FirstCommand : Command
{
}

public class SecondCommand : Command
{
}

public class ThirdCommand : Command
{
}
```

Next, as you would have done normally create the handlers for your commands. 
The only difference is that the handlers need to implement the **ISequenceCommandHandlerAsync<>** interface.
The _HandlerAsync_ of this interface accepts an extra parameter which is a _CommandResponse_.
OpenCQRS will pass automatically the command response of the previous command in the sequence.
For the first handler it would obviously be null.

First command handler:

```C#
public class FirstCommandHandler : ISequenceCommandHandlerAsync<FirstCommand>
{
    public Task<CommandResponse> HandleAsync(FirstCommand command, CommandResponse previousStepResponse)
    {
        Console.WriteLine("Message from first command handler");

        return Task.FromResult(new CommandResponse
        { 
            Result = "First result"
        });
    }
}
```

Second command handler:

```C#
public class SecondCommandHandler : ISequenceCommandHandlerAsync<SecondCommand>
{
    public Task<CommandResponse> HandleAsync(SecondCommand command, CommandResponse previousStepResponse)
    {
        Console.WriteLine($"Message from second command handler. Result from first handler: {previousStepResponse.Result}");

        return Task.FromResult(new CommandResponse
        {
            Result = "Second result"
        });
    }
}
```

Third command handler:

```C#
public class ThirdCommandHandler : ISequenceCommandHandlerAsync<ThirdCommand>
{
    public Task<CommandResponse> HandleAsync(ThirdCommand command, CommandResponse previousStepResponse)
    {
        Console.WriteLine($"Message from third command handler. Result from second handler: {previousStepResponse.Result}");

        return Task.FromResult(new CommandResponse
        {
            Result = "Third result"
        });
    }
}
```

Last, create a class that inherits from the _CommandSequence_ abstract class and add all your commands:

```C#
public class SampleCommandSequence : CommandSequence
{
    public SampleCommandSequence()
    {
        AddCommand(new FirstCommand());
        AddCommand(new SecondCommand());
        AddCommand(new ThirdCommand());
    }
}
```

Use the dispatcher to execute the command sequence:

```C#
await dispatcher.SendAsync(new SampleCommandSequence());
```

You can also get a result from the sequence which will be the value of the Result property of the command response of the last command in the sequence (in our example of the ThirdCommand):

```C#
var result = await dispatcher.SendAsync<string>(new SampleCommandSequence());
```

The following is the output generated:

```
Message from first command handler
Message from second command handler. Result from first handler: First result
Message from third command handler. Result from second handler: Second result
Final result: Third result
```

You can find the sample code [here](https://github.com/lucabriguglia/OpenCQRS/tree/master/samples/OpenCqrs.Sample.CommandSequence).

## Related

- [Commands](Commands)
- [Command Validation](Command-Validation)
- [Domain Commands With Event Sourcing](With-Event-Sourcing)
- [Domain Commands Without Event Sourcing](Without-Event-Sourcing)
