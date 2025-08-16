using System;
using OpenCqrs.Queries;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;

namespace OpenCqrs.Sample.EventSourcing.Reporting.Queries
{
    public class GetProduct : Query<ProductEntity>
    {
        public Guid ProductId { get; set; }
    }
}
