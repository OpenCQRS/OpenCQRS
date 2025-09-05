using System.Collections.Concurrent;
using OpenCqrs.Caching;
using OpenCqrs.Results;

namespace OpenCqrs.Queries;

/// <summary>
/// Provides query processing functionality by dispatching queries to their corresponding handlers.
/// Uses caching and reflection to efficiently resolve and execute query handlers from the service provider.
/// </summary>
/// <example>
/// <code>
/// var processor = new QueryProcessor(serviceProvider);
/// var result = await processor.Get(new GetUserQuery { UserId = userId });
/// </code>
/// </example>
public class QueryProcessor(IServiceProvider serviceProvider, ICachingService cachingService) : IQueryProcessor
{
    private static readonly ConcurrentDictionary<Type, object?> QueryHandlerWrappers = new();

    /// <summary>
    /// Executes a query and returns the requested data by dispatching it to the appropriate handler.
    /// </summary>
    /// <typeparam name="TResult">The type of data expected from the query.</typeparam>
    /// <param name="query">The query instance to be executed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the query result on success or failure information.</returns>
    public async Task<Result<TResult>> Get<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();

        var handler = (QueryHandlerWrapperBase<TResult>)QueryHandlerWrappers.GetOrAdd(queryType, _ =>
            Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(queryType, typeof(TResult))))!;

        if (query is not CacheableQuery<TResult> cacheableQuery)
        {
            return await handler.Handle(query, serviceProvider, cancellationToken);
        }
        
        if (string.IsNullOrEmpty(cacheableQuery.CacheKey))
        {
            throw new Exception("No Cache Key was provided");
        }
        
        var result = await cachingService.GetOrSet(cacheableQuery.CacheKey, 
            () => handler.Handle(query, serviceProvider, cancellationToken), 
            cacheableQuery.CacheTimeInSeconds);

        return result ?? new Failure();
    }
}
