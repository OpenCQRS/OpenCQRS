# UI

The UI package is an experimental package that can be used to display information about an aggregate and its events:

```C#
var model = await _dispatcher.GetResultAsync(new GetAggregateModel
{
    AggregateRootId = id
});
```

The model will contain the list of all events of the aggregate ordered by time stamp descending.
Further improvements will be made in coming versions.

You can find a sample usage [here](https://github.com/lucabriguglia/OpenCQRS/blob/master/samples/OpenCqrs.Sample.EventSourcing/Pages/Edit.cshtml.cs).
