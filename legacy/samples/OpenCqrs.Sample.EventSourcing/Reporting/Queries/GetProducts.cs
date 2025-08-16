using System.Collections.Generic;
using OpenCqrs.Queries;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.Queries
{
    public class GetProducts : CacheableQuery<IList<ProductEntity>>
    {
        public GetProducts()
        {
            CacheKey = CacheKeys.ProductsCacheKey;
            CacheTime = 10;
        }
    }
}
