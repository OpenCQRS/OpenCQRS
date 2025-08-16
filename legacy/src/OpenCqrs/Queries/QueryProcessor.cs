using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenCqrs.Caching;
using OpenCqrs.Dependencies;
using OpenCqrs.Exceptions;

namespace OpenCqrs.Queries
{
    /// <inheritdoc />
    public class QueryProcessor : IQueryProcessor
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly ICacheManager _cacheManager;
        private readonly CacheOptions _cacheOptions;

        private static readonly ConcurrentDictionary<Type, object> QueryHandlerWrappers = new();

        public QueryProcessor(IHandlerResolver handlerResolver, 
            ICacheManager cacheManager, 
            IOptions<CacheOptions> cacheOptions)
        {
            _handlerResolver = handlerResolver;
            _cacheManager = cacheManager;
            _cacheOptions = cacheOptions.Value;
        }

        /// <inheritdoc />
        public Task<TResult> ProcessAsync<TResult>(IQuery<TResult> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            Task<TResult> GetResultAsync(IQuery<TResult> q)
            {
                var queryType = q.GetType();
                var handler = (QueryHandlerWrapperBase<TResult>)QueryHandlerWrappers.GetOrAdd(queryType,
                    t => Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(queryType, typeof(TResult))));
                return handler.HandleAsync(q, _handlerResolver);
            }

            if (query is not ICacheableQuery<TResult> cacheableQuery)
            {
                return GetResultAsync(query);
            }

            if (string.IsNullOrEmpty(cacheableQuery.CacheKey))
            {
                throw new QueryException("Cache key is required.");
            }

            return _cacheManager.GetOrSetAsync(
                cacheableQuery.CacheKey, 
                cacheableQuery.CacheTime ?? _cacheOptions.DefaultCacheTime, 
                () => GetResultAsync(query));
        }

        /// <inheritdoc />
        public TResult Process<TResult>(IQuery<TResult> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            TResult GetResult(IQuery<TResult> q)
            {
                var queryType = q.GetType();
                var handler = (QueryHandlerWrapperBase<TResult>)QueryHandlerWrappers.GetOrAdd(queryType,
                    t => Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(queryType, typeof(TResult))));
                return handler.Handle(q, _handlerResolver);
            }

            if (query is not ICacheableQuery<TResult> cacheableQuery)
            {
                return GetResult(query);
            }

            if (string.IsNullOrEmpty(cacheableQuery.CacheKey))
            {
                throw new QueryException("Cache key is required.");
            }

            return _cacheManager.GetOrSet(
                cacheableQuery.CacheKey,
                cacheableQuery.CacheTime ?? _cacheOptions.DefaultCacheTime,
                () => GetResult(query));
        }
    }
}