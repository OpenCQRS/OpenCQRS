using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Queries;
using OpenCqrs.Sample.NoEventSourcing.Data;
using OpenCqrs.Sample.NoEventSourcing.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Reporting.Handlers
{
    public class GetProductHandler : IQueryHandlerAsync<GetProduct, Product>
    {
        private readonly SampleDbContext _dbContext;

        public GetProductHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Product> HandleAsync(GetProduct query)
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
