using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace OpenCqrs.Caching.Memory;

public class MemoryCacheProvider(IMemoryCache memoryCache, IOptions<Configuration.MemoryCacheOptions> options) : ICachingProvider
{
    public Task<T?> Get<T>(string key)
    {
        var data = memoryCache.Get<T>(key);
        return Task.FromResult(data);
    }

    public async Task Set(string key, object? data, int? cacheTimeInSeconds = null)
    {
        if (data is null)
        {
            return;
        }

        var isSet = await IsSet(key);
        if (isSet)
        {
            return;
        }

        cacheTimeInSeconds ??= options.Value.DefaultCacheTimeInSeconds;
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheTimeInSeconds.Value));

        memoryCache.Set(key, data, memoryCacheEntryOptions);
    }

    public Task<bool> IsSet(string key)
    {
        var ieSet = memoryCache.Get(key) is not null;
        return Task.FromResult(ieSet);
    }

    public Task Remove(string key)
    {
        memoryCache.Remove(key);
        return Task.FromResult(true);
    }
}
