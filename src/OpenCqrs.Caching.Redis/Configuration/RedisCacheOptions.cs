namespace OpenCqrs.Caching.Redis.Configuration;

public class RedisCacheOptions
{
    public int DefaultCacheTimeInSeconds { get; set; } = 60;
    public required string ConnectionString { get; set; }
    public int Db { get; set; } = -1;
    public object? AsyncState { get; set; } = null;
}
