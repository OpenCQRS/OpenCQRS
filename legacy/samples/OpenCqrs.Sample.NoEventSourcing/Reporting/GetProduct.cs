using System;
using OpenCqrs.Queries;
using OpenCqrs.Sample.NoEventSourcing.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Reporting
{
    public class GetProduct : Query<Product>
    {
        public Guid ProductId { get; set; }
    }
}
