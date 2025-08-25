# Commands

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
