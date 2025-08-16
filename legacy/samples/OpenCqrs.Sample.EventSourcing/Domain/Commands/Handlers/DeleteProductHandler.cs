using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Domain;

namespace OpenCqrs.Sample.EventSourcing.Domain.Commands.Handlers
{
    public class DeleteProductHandler : ICommandHandlerAsync<DeleteProduct>
    {
        private readonly IRepository<Product> _repository;

        public DeleteProductHandler(IRepository<Product> repository)
        {
            _repository = repository;
        }

        public async Task<CommandResponse> HandleAsync(DeleteProduct command)
        {
            var product = await _repository.GetByIdAsync(command.AggregateRootId);

            if (product == null)
            {
                throw new ApplicationException($"Product not found. Id: {command.AggregateRootId}");
            }

            product.Delete();

            return new CommandResponse
            {
                Events = product.Events
            };
        }
    }
}
