using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Domain;

namespace OpenCqrs.Sample.EventSourcing.Domain.Commands.Handlers
{
    public class PublishProductHandler : ICommandHandlerAsync<PublishProduct>
    {
        private readonly IRepository<Product> _repository;

        public PublishProductHandler(IRepository<Product> repository)
        {
            _repository = repository;
        }

        public async Task<CommandResponse> HandleAsync(PublishProduct command)
        {
            var product = await _repository.GetByIdAsync(command.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {command.AggregateRootId}");
            }

            product.Publish();

            return new CommandResponse
            {
                Events = product.Events,
                Result = true
            };
        }
    }
}
