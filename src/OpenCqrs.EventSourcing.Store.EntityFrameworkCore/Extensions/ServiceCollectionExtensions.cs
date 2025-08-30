using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsEntityFrameworkCore<TDbContext>(this IServiceCollection services) where TDbContext : IDomainDbContext
    {
        services.AddScoped<IDomainDbContext>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());
        services.TryAddScoped<IDomainService, EntityFrameworkCoreDomainService>();
    }
}
