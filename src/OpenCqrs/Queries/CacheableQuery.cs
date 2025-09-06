namespace OpenCqrs.Queries;

public abstract class CacheableQuery<TResult> : IQuery<TResult>
{
    /// <summary>
    /// The value indicating the cache key to use if retrieving from the cache.
    /// </summary>
    public required string CacheKey { get; set; }

    /// <summary>
    /// The value indicating the cache time (in seconds). If not set, the value will be taken from configured options.
    /// </summary>
    public int? CacheTimeInSeconds { get; set; }
}
