using System;
using System.Threading.Tasks;

namespace OpenCqrs.Caching
{
    public interface ICacheProvider
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync(string key, int cacheTime, object data);
        Task<bool> IsSetAsync(string key);
        Task RemoveAsync(string key);
        T Get<T>(string key);
        void Set(string key, int cacheTime, object data);
        bool IsSet(string key);
        void Remove(string key);
    }

    public class DefaultCacheProvider : ICacheProvider
    {
        public T Get<T>(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public Task<T> GetAsync<T>(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public bool IsSet(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public Task<bool> IsSetAsync(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public void Remove(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public Task RemoveAsync(string key)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public void Set(string key, int cacheTime, object data)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }

        public Task SetAsync(string key, int cacheTime, object data)
        {
            throw new NotImplementedException(Consts.CacheProviderRequiredMessage);
        }
    }
}
