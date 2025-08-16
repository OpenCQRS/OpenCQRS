using Microsoft.AspNetCore.Builder;

namespace OpenCqrs.Extensions
{
    public interface IOpenCqrsAppBuilder
    {
        IApplicationBuilder App { get; }
    }
}