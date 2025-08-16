using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Queries;
using OpenCqrs.Sample.EventSourcing.Domain;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;
using OpenCqrs.Sample.EventSourcing.Reporting.Queries;

namespace OpenCqrs.Sample.EventSourcing.Reporting.QueryHandlers
{
    public class GetPublishedProductsHandler : IQueryHandlerAsync<GetPublishedProducts, IEnumerable<ProductEntity>>
    {
        private readonly ReportingDbContext _dbContext;

        public GetPublishedProductsHandler(ReportingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<ProductEntity>> HandleAsync(GetPublishedProducts query)
        {
            return await _dbContext.Products.Where(x => x.Status == ProductStatus.Published).ToListAsync();
        }
    }
}
