using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Domain;
using OpenCqrs.Sample.NoEventSourcing.Data;
using OpenCqrs.Sample.NoEventSourcing.Domain.Events;

namespace OpenCqrs.Sample.NoEventSourcing.Domain.Commands.Handlers
{
    public class CreateProductHandler : ICommandHandlerAsync<CreateProduct>
    {
        private readonly SampleDbContext _dbContext;

        public CreateProductHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CommandResponse> HandleAsync(CreateProduct command)
        {
            var product = new Product(command.Name, command.Description, command.Price);

            _dbContext.Products.Add(product);

            await _dbContext.SaveChangesAsync();

            return new CommandResponse
            {
                Events = new List<IDomainEvent>()
                {
                    new ProductCreated
                    {
                        AggregateRootId = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price
                    }
                }
            };
        }
    }
}
