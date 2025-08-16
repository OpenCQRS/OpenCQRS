using System;
using System.Threading.Tasks;

namespace OpenCqrs.Domain
{
    public interface IRepository<T> where T : IAggregateRoot
    {
        /// <summary>
        /// Saves the specified aggregate asynchronous.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <returns></returns>
        Task SaveAsync(T aggregate);

        /// <summary>
        /// Saves the specified aggregate.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        void Save(T aggregate);

        /// <summary>
        /// Gets the by identifier asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        Task<T> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets the by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        T GetById(Guid id);       
    }
}