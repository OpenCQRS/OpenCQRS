using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Sample.EventSourcing.Domain.Commands;

namespace OpenCqrs.Sample.EventSourcing.Domain
{
    public class ProductService : IProductService
    {
        public async Task<CommandResponse> CreateProductAsync(CreateProduct command)
        {
            var product = new Product(command.AggregateRootId, command.Name, command.Description, command.Price);

            return await Task.FromResult(new CommandResponse
            {
                Events = product.Events,
                Result = true
            });
        }
    }
}
