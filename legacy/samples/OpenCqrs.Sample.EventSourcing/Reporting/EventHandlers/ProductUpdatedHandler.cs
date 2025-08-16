using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Events;
using OpenCqrs.Sample.EventSourcing.Domain.Events;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.EventHandlers
{
    public class ProductUpdatedHandler : IEventHandlerAsync<ProductUpdated>
    {
        private readonly ReportingDbContext _dbContext;

        public ProductUpdatedHandler(ReportingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task HandleAsync(ProductUpdated @event)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == @event.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {@event.AggregateRootId}");
            }

            product.Name = @event.Name;
            product.Description = @event.Description;
            product.Price = @event.Price;

            await _dbContext.SaveChangesAsync();
        }
    }
}
