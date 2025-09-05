namespace OpenCqrs.Caching;

public interface ICachingProvider
{
    Task<T> Get<T>(string key);
    Task Set(string key, object? data, int? cacheTimeInSeconds = null);
    Task<bool> IsSet(string key);
    Task Remove(string key);
}
