﻿using Microsoft.AspNetCore.Builder;

namespace Kledex.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IKledexAppBuilder UseKledex(this IApplicationBuilder app)
        {
            return new KledexAppBuilder(app);
        }
    }
}