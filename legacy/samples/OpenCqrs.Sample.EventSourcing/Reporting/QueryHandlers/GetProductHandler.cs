using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Queries;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;
using OpenCqrs.Sample.EventSourcing.Reporting.Queries;

namespace OpenCqrs.Sample.EventSourcing.Reporting.QueryHandlers
{
    public class GetProductHandler : IQueryHandlerAsync<GetProduct, ProductEntity>
    {
        private readonly ReportingDbContext _dbContext;

        public GetProductHandler(ReportingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProductEntity> HandleAsync(GetProduct query)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == query.ProductId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {query.ProductId}");
            }

            return product;
        }
    }
}
