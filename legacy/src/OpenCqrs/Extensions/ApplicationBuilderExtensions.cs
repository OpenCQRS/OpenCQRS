using Microsoft.AspNetCore.Builder;

namespace OpenCqrs.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IOpenCqrsAppBuilder UseOpenCqrs(this IApplicationBuilder app)
        {
            return new OpenCqrsAppBuilder(app);
        }
    }
}