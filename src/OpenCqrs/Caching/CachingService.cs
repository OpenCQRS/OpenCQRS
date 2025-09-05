using OpenCqrs.Results;

namespace OpenCqrs.Caching;

public class CachingService(ICachingProvider cachingProvider) : ICachingService
{
    public async Task<T?> GetOrSet<T>(string key, Func<Task<T>> acquire, int? cacheTimeInSeconds = null)
    {
        var data = await cachingProvider.Get<T>(key);
        if (data != null)
        {
            return data;
        }

        var result = await acquire();
        if (result == null)
        {
            return default;
        }

        await cachingProvider.Set(key, result, cacheTimeInSeconds);

        return result;
    }

    public async Task Remove(string key)
    {
        await cachingProvider.Remove(key);
    }
}
