using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsEntityFrameworkCore<T>(this IServiceCollection services) where T : IDomainDbContext
    {
        services.TryAddScoped<IDomainService, EntityFrameworkCoreDomainService>();
        services.AddScoped<IDomainDbContext>(provider => provider.GetRequiredService<T>());
    }
}
