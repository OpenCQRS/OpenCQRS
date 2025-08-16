using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenCqrs.Domain
{
    public interface IStoreProvider
    {
        /// <summary>
        /// Saves the store data asynchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        Task SaveAsync(SaveStoreData request);

        /// <summary>
        /// Gets the events asynchronously.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId);

        /// <summary>
        /// Saves the domain data.
        /// </summary>
        /// <param name="request">The request.</param>
        void Save(SaveStoreData request);

        /// <summary>
        /// Gets the events.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        IEnumerable<IDomainEvent> GetEvents(Guid aggregateId);
    }

    public class DefaultStoreProvider : IStoreProvider
    {
        public IEnumerable<IDomainEvent> GetEvents(Guid aggregateId)
        {
            throw new NotImplementedException(Consts.StoreRequiredMessage);
        }

        public Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId)
        {
            throw new NotImplementedException(Consts.StoreRequiredMessage);
        }

        public void Save(SaveStoreData request)
        {
            throw new NotImplementedException(Consts.StoreRequiredMessage);
        }

        public Task SaveAsync(SaveStoreData request) 
        {
            throw new NotImplementedException(Consts.StoreRequiredMessage);
        }
    }
}
