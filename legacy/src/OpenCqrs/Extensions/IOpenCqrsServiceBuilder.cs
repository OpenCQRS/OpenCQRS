using Microsoft.Extensions.DependencyInjection;

namespace OpenCqrs.Extensions
{
    public interface IOpenCqrsServiceBuilder
    {
        IServiceCollection Services { get; }
    }
}
