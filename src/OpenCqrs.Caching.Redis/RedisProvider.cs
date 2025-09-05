namespace OpenCqrs.Caching.Redis;

public class RedisProvider : ICachingProvider
{
    public Task<T> Get<T>(string key)
    {
        throw new NotImplementedException();
    }

    public Task Set(string key, object? data, int? cacheTimeInSeconds = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsSet(string key)
    {
        throw new NotImplementedException();
    }

    public Task Remove(string key)
    {
        throw new NotImplementedException();
    }
}
