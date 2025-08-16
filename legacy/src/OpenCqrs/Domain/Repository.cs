using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenCqrs.Domain
{
    /// <inheritdoc />
    public class Repository<T> : IRepository<T> where T : IAggregateRoot
    {
        private readonly IStoreProvider _storeProvider;

        public Repository(IStoreProvider storeProvider)
        {
            _storeProvider = storeProvider;
        }

        /// <inheritdoc />
        public async Task SaveAsync(T aggregate)
        {
            await _storeProvider.SaveAsync(new SaveStoreData
            {
                AggregateType = typeof(T),
                AggregateRootId = aggregate.Id,
                Events = aggregate.Events,
                DomainCommand = null
            });
        }

        /// <inheritdoc />
        public void Save(T aggregate)
        {
            _storeProvider.Save(new SaveStoreData
            {
                AggregateType = typeof(T),
                AggregateRootId = aggregate.Id,
                Events = aggregate.Events,
                DomainCommand = null
            });
        }

        /// <inheritdoc />
        public async Task<T> GetByIdAsync(Guid id)
        {
            var events = await _storeProvider.GetEventsAsync(id);
            var domainEvents = events as DomainEvent[] ?? events.ToArray();
            if (!domainEvents.Any())
            {
                return default;
            }

            var aggregate = Activator.CreateInstance<T>();        
            aggregate.LoadsFromHistory(domainEvents);
            return aggregate;
        }

        /// <inheritdoc />
        public T GetById(Guid id)
        {
            var events = _storeProvider.GetEvents(id);
            var domainEvents = events as DomainEvent[] ?? events.ToArray();
            if (!domainEvents.Any())
            {
                return default;
            }

            var aggregate = Activator.CreateInstance<T>();          
            aggregate.LoadsFromHistory(domainEvents);
            return aggregate;
        }
    }
}