# Caching Queries

With OpenCQRS it is possible to automatically cache the result of a query using one of the available providers (Memory or Redis). First, you need to configure a cache provider as explained [here](Configuration#caching). Next, as per any normal queries, create the model for the result and the query. The only difference with a normal query is that it needs to inherit from the **CacheableQuery<>** abstract class (or implements the **ICacheableQuery<>** interface) in order to set the _Cache Key_ and the _Cache Time_:

```C#
public class Something
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GetSomething : CacheableQuery<Something>
{
    public int Id { get; set; }
    
    public GetSomething(int id)
    {
        CacheKey = $"Something | Id: {id}"; // required
        CacheTime = 600; // optional, default value can be set when registering a provider (in seconds)
    }
}
```

The handler would be the same as any other query handler, it would know nothing about the cache:

```C#
public class GetSomethingQueryHandler : IQueryHandlerAsync<GetSomething, Something>
{
    private readonly MyDbContext _dbContext;

    public GetProductsHandler(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
        
    public Task<Something> HandleAsync(GetSomething query)
    {
        return _dbContext.Somethings.FirstOrDefaultAsync(x => x.Id == query.Id);
    }
}
```

Get the cached result using the dispatcher:

```C#
var query = new GetSomething(123);
var something = await _dispatcher.GetResultAsync(query);
```

As you can see the only difference is in the query class.

## Related

- [Queries](Queries)
