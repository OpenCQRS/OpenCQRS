using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Queries;
using OpenCqrs.Sample.NoEventSourcing.Data;
using OpenCqrs.Sample.NoEventSourcing.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Reporting.Handlers
{
    public class GetProductsHandler : IQueryHandlerAsync<GetProducts, IList<Product>>
    {
        private readonly SampleDbContext _dbContext;

        public GetProductsHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IList<Product>> HandleAsync(GetProducts query)
        {
            return await _dbContext.Products.Where(x => x.Status != ProductStatus.Deleted).ToListAsync();
        }
    }
}
