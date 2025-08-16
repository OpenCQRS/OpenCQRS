namespace OpenCqrs.Queries
{
    public interface ICacheableQuery<TResult> : IQuery<TResult>
    {
        /// <summary>
        /// The value indicating the cache key to use if retrieving from cache.
        /// </summary>
        string CacheKey { get; set; }

        /// <summary>
        /// The value indicating the cache time (in seconds). Default value is 60.
        /// </summary>
        int? CacheTime { get; set; }
    }
}
