using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Sample.EventSourcing.Domain.Commands;

namespace OpenCqrs.Sample.EventSourcing.Domain
{
    public interface IProductService
    {
        Task<CommandResponse> CreateProductAsync(CreateProduct command);
    }
}
