using System;
using System.Threading.Tasks;

namespace OpenCqrs.Caching
{
    public interface ICacheManager
    {
        /// <summary>Gets or sets data asynchronously.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="acquireAsync">The acquire asynchronous.</param>
        /// <returns></returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> acquireAsync);

        /// <summary>Gets or sets data asynchronously.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="cacheTime"></param>
        /// <param name="acquireAsync">The acquire asynchronous.</param>
        /// <returns></returns>
        Task<T> GetOrSetAsync<T>(string key, int cacheTime, Func<Task<T>> acquireAsync);

        /// <summary>Removes data from cache asynchronously.</summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        Task RemoveAsync(string key);

        /// <summary>Gets or sets data.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="acquire">The acquire.</param>
        /// <returns></returns>
        T GetOrSet<T>(string key, Func<T> acquire);

        /// <summary>Gets or sets data.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="cacheTime">The cache time.</param>
        /// <param name="acquire">The acquire.</param>
        /// <returns></returns>
        T GetOrSet<T>(string key, int cacheTime, Func<T> acquire);

        /// <summary>Removes data from cache.</summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        void Remove(string key);
    }
}
