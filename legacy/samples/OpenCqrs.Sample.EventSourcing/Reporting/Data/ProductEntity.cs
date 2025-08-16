using System;
using OpenCqrs.Sample.EventSourcing.Domain;

namespace OpenCqrs.Sample.EventSourcing.Reporting.Data
{
    public class ProductEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
    }
}
