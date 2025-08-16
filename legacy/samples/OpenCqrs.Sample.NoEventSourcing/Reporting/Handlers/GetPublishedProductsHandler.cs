using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Queries;
using OpenCqrs.Sample.NoEventSourcing.Data;
using OpenCqrs.Sample.NoEventSourcing.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Reporting.Handlers
{
    public class GetPublishedProductsHandler : IQueryHandlerAsync<GetPublishedProducts, IEnumerable<Product>>
    {
        private readonly SampleDbContext _dbContext;

        public GetPublishedProductsHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Product>> HandleAsync(GetPublishedProducts query)
        {
            return await _dbContext.Products.Where(x => x.Status == ProductStatus.Published).ToListAsync();
        }
    }
}
