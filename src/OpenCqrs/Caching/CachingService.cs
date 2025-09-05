namespace OpenCqrs.Caching;

public class CachingService(ICachingProvider cachingProvider) : ICachingService
{
    public async Task<T?> GetOrSet<T>(string key, Func<Task<T>> acquireAsync, int? cacheTimeInSeconds = null)
    {
        var data = await cachingProvider.Get<T>(key);
        if (data != null)
        {
            return data;
        }

        var result = await acquireAsync();
        if (result == null)
        {
            return default;
        }

        await cachingProvider.Set(key, result, cacheTimeInSeconds);

        return result;
    }

    public Task Remove(string key)
    {
        return cachingProvider.Remove(key);
    }
}
