using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace OpenCqrs.Caching
{
    public class CacheManager : ICacheManager
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly CacheOptions _options;

        public CacheManager(ICacheProvider cacheProvider, IOptions<CacheOptions> options)
        {
            _cacheProvider = cacheProvider;
            _options = options.Value;
        }

        /// <inheritdoc />
        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> acquireAsync)
        {
            return GetOrSetAsync(key, _options.DefaultCacheTime, acquireAsync);
        }

        /// <inheritdoc />
        public async Task<T> GetOrSetAsync<T>(string key, int cacheTime, Func<Task<T>> acquireAsync)
        {
            var data = await _cacheProvider.GetAsync<T>(key);

            if (data != null)
            {
                return data;
            }

            var result = await acquireAsync();

            await _cacheProvider.SetAsync(key, cacheTime, result);

            return result;
        }

        /// <inheritdoc />
        public Task RemoveAsync(string key)
        {
            return _cacheProvider.RemoveAsync(key);
        }

        /// <inheritdoc />
        public T GetOrSet<T>(string key, Func<T> acquire)
        {
            return GetOrSet(key, _options.DefaultCacheTime, acquire);
        }

        /// <inheritdoc />
        public T GetOrSet<T>(string key, int cacheTime, Func<T> acquire)
        {
            var data = _cacheProvider.Get<T>(key);

            if (data != null)
            {
                return data;
            }

            var result = acquire();

            _cacheProvider.Set(key, cacheTime, result);

            return result;
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            _cacheProvider.Remove(key);
        }
    }
}
