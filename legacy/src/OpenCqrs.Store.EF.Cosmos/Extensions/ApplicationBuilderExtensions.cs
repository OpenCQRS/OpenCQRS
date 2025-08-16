using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Extensions;

namespace OpenCqrs.Store.EF.Cosmos.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IOpenCqrsAppBuilder EnsureCosmosDbCreated(this IOpenCqrsAppBuilder builder)
        {
            var dbContextFactory = builder.App.ApplicationServices.GetService<IDomainDbContextFactory>();

            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                dbContext.Database.EnsureCreated();
            }

            return builder;
        }
    }
}