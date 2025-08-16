using System;
using Microsoft.AspNetCore.Builder;

namespace OpenCqrs.Extensions
{
    public class OpenCqrsAppBuilder : IOpenCqrsAppBuilder
    {
        public IApplicationBuilder App { get; }

        public OpenCqrsAppBuilder(IApplicationBuilder app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
        }
    }
}