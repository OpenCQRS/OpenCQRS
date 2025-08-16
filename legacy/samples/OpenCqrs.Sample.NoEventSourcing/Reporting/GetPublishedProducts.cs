using System.Collections.Generic;
using OpenCqrs.Queries;
using OpenCqrs.Sample.NoEventSourcing.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Reporting
{
    public class GetPublishedProducts : Query<IEnumerable<Product>>
    {
    }
}
