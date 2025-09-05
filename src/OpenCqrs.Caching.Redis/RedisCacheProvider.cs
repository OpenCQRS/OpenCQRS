using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenCqrs.Caching.Redis.Configuration;
using StackExchange.Redis;

namespace OpenCqrs.Caching.Redis;

public class RedisCacheProvider(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisCacheOptions> options) : ICachingProvider
{
    private IDatabase Database => connectionMultiplexer.GetDatabase(options.Value.Db, options.Value.AsyncState);

    public async Task<T?> Get<T>(string key)
    {
        var value = await Database.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonConvert.DeserializeObject<T>(value!);
    }

    public async Task Set(string key, object? data, int? cacheTimeInSeconds = null)
    {
        var json = JsonConvert.SerializeObject(data);
        cacheTimeInSeconds ??= options.Value.DefaultCacheTimeInSeconds;
        await Database.StringSetAsync(key, json, TimeSpan.FromSeconds(cacheTimeInSeconds.Value));
    }

    public async Task<bool> IsSet(string key)
    {
        return await Database.KeyExistsAsync(key);
    }

    public async Task Remove(string key)
    {
        await Database.KeyDeleteAsync(key);
    }
}
