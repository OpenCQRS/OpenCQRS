using System.Collections.Generic;
using OpenCqrs.Queries;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.Queries
{
    public class GetPublishedProducts : Query<IEnumerable<ProductEntity>>
    {
    }
}
