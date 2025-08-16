using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Events;
using OpenCqrs.Sample.EventSourcing.Domain;
using OpenCqrs.Sample.EventSourcing.Domain.Events;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.EventHandlers
{
    public class ProductPublishedHandler : IEventHandlerAsync<ProductPublished>
    {
        private readonly ReportingDbContext _dbContext;

        public ProductPublishedHandler(ReportingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task HandleAsync(ProductPublished @event)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == @event.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {@event.AggregateRootId}");
            }

            product.Status = ProductStatus.Published;

            await _dbContext.SaveChangesAsync();
        }
    }
}
