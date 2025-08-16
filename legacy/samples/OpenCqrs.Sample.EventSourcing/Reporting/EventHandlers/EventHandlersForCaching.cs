using System.Threading.Tasks;
using OpenCqrs.Caching;
using OpenCqrs.Events;
using OpenCqrs.Sample.EventSourcing.Domain.Events;

namespace OpenCqrs.Sample.EventSourcing.Reporting.EventHandlers
{
    public class EventHandlersForCaching : 
        IEventHandlerAsync<ProductCreated>,
        IEventHandlerAsync<ProductDeleted>,
        IEventHandlerAsync<ProductPublished>,
        IEventHandlerAsync<ProductUpdated>,
        IEventHandlerAsync<ProductWithdrew>
    {
        private readonly ICacheManager _cacheManager;

        public EventHandlersForCaching(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public Task HandleAsync(ProductCreated @event)
        {
            return RemoveCacheAsync();
        }

        public Task HandleAsync(ProductDeleted @event)
        {
            return RemoveCacheAsync();
        }

        public Task HandleAsync(ProductPublished @event)
        {
            return RemoveCacheAsync();
        }

        public Task HandleAsync(ProductUpdated @event)
        {
            return RemoveCacheAsync();
        }

        public Task HandleAsync(ProductWithdrew @event)
        {
            return RemoveCacheAsync();
        }

        private Task RemoveCacheAsync()
        {
            return _cacheManager.RemoveAsync(CacheKeys.ProductsCacheKey);
        }
    }
}
