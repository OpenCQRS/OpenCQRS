using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace OpenCqrs.Caching.Memory
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            await Task.CompletedTask;
            return Get<T>(key);
        }

        public async Task SetAsync(string key, int cacheTime, object data)
        {
            await Task.CompletedTask;
            Set(key, cacheTime, data);
        }

        public async Task<bool> IsSetAsync(string key)
        {
            await Task.CompletedTask;
            return IsSet(key);
        }

        public async Task RemoveAsync(string key)
        {
            await Task.CompletedTask;
            Remove(key);
        }

        public T Get<T>(string key)
        {
            return (T)_memoryCache.Get(key);
        }

        public void Set(string key, int cacheTime, object data)
        {
            if (data == null)
            {
                return;
            }

            if (IsSet(key))
            {
                return;
            }

            var memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheTime));

            _memoryCache.Set(key, data, memoryCacheEntryOptions);
        }

        public bool IsSet(string key)
        {
            return _memoryCache.Get(key) != null;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }
    }
}
