using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Events;
using OpenCqrs.Sample.EventSourcing.Domain;
using OpenCqrs.Sample.EventSourcing.Domain.Events;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.EventHandlers
{
    public class ProductWithdrewHandler : IEventHandlerAsync<ProductWithdrew>
    {
        private readonly ReportingDbContext _dbContext;

        public ProductWithdrewHandler(ReportingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task HandleAsync(ProductWithdrew @event)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == @event.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {@event.AggregateRootId}");
            }

            product.Status = ProductStatus.Draft;

            await _dbContext.SaveChangesAsync();
        }
    }
}
