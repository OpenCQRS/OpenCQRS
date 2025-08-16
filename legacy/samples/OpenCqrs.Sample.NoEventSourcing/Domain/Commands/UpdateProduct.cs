using OpenCqrs.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Domain.Commands
{
    public class UpdateProduct : DomainCommand<Product>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}
