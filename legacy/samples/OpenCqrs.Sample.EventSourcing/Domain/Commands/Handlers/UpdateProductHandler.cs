using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Domain;

namespace OpenCqrs.Sample.EventSourcing.Domain.Commands.Handlers
{
    public class UpdateProductHandler : ICommandHandlerAsync<UpdateProduct>
    {
        private readonly IRepository<Product> _repository;

        public UpdateProductHandler(IRepository<Product> repository)
        {
            _repository = repository;
        }

        public async Task<CommandResponse> HandleAsync(UpdateProduct command)
        {
            var product = await _repository.GetByIdAsync(command.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {command.AggregateRootId}");
            }

            product.Update(command.Name, command.Description, command.Price);

            return new CommandResponse
            {
                Events = product.Events
            };
        }
    }
}
