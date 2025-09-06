using OpenCqrs.Results;

namespace OpenCqrs.Caching;

public interface ICachingService
{
    Task<T?> GetOrSet<T>(string key, Func<Task<T>> acquire, int? cacheTimeInSeconds = null);
    Task Remove(string key);
}
