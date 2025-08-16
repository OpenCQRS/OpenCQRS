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
    public class PublishProductHandler : ICommandHandlerAsync<PublishProduct>
    {
        private readonly SampleDbContext _dbContext;

        public PublishProductHandler(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CommandResponse> HandleAsync(PublishProduct command)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == command.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {command.AggregateRootId}");
            }

            product.Publish();

            await _dbContext.SaveChangesAsync();

            return new CommandResponse
            {
                Events = new List<IDomainEvent>()
                {
                    new ProductPublished
                    {
                        AggregateRootId = product.Id
                    }
                }
            };
        }
    }
}
