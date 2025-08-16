using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.Commands;
using OpenCqrs.Domain;
using OpenCqrs.Sample.NoEventSourcing.Data;
using OpenCqrs.Sample.NoEventSourcing.Domain.Events;

namespace OpenCqrs.Sample.NoEventSourcing.Domain.Commands.Handlers
{
    public class UpdateProductHandler : ICommandHandlerAsync<UpdateProduct>
    {
        private readonly SampleDbContext _dbContext;

        public UpdateProductHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CommandResponse> HandleAsync(UpdateProduct command)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == command.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {command.AggregateRootId}");
            }

            product.Update(command.Name, command.Description, command.Price);

            await _dbContext.SaveChangesAsync();

            return new CommandResponse
            {
                Events = new List<IDomainEvent>()
                {
                    new ProductUpdated
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
