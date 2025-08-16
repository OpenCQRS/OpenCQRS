using OpenCqrs.Domain;

namespace OpenCqrs.Sample.NoEventSourcing.Domain
{
    public class Product : AggregateRoot
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }

        public Product()
        {
        }

        public Product(string name, string description, decimal price)
        {
            Name = name;
            Description = description;
            Price = price;
            Status = ProductStatus.Draft;
        }

        public void Update(string name, string description, decimal price)
        {
            Name = name;
            Description = description;
            Price = price;
        }

        public void Publish()
        {
            Status = ProductStatus.Published;
        }

        public void Withdraw()
        {
            Status = ProductStatus.Draft;
        }

        public void Delete()
        {
            Status = ProductStatus.Deleted;
        }
    }
}
