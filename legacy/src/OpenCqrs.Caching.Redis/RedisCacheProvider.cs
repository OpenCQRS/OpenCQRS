using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace OpenCqrs.Caching.Redis
{
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly int _db;
        private readonly object _asyncState;

        private IDatabase Database => _connectionMultiplexer.GetDatabase(_db, _asyncState);

        public RedisCacheProvider(IConnectionMultiplexer connectionMultiplexer, int db = -1, object asyncState = null)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _db = db;
            _asyncState = asyncState;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await Database.StringGetAsync(key);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value);
        }

        public async Task SetAsync(string key, int cacheTime, object data)
        {
            var json = JsonSerializer.Serialize(data);
            await Database.StringSetAsync(key, json, TimeSpan.FromSeconds(cacheTime));
        }

        public Task<bool> IsSetAsync(string key)
        {
            return Database.KeyExistsAsync(key);
        }

        public Task RemoveAsync(string key)
        {
            return Database.KeyDeleteAsync(key);
        }

        public T Get<T>(string key)
        {
            var value = Database.StringGet(key);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value);
        }

        public void Set(string key, int cacheTime, object data)
        {
            var json = JsonSerializer.Serialize(data);
            Database.StringSet(key, json, TimeSpan.FromSeconds(cacheTime));
        }

        public bool IsSet(string key)
        {
            return Database.KeyExists(key);
        }

        public void Remove(string key)
        {
            Database.KeyDelete(key);
        }
    }
}
