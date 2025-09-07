using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCqrs.EventSourcing.DomainService;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsEntityFrameworkCore<TDbContext>(this IServiceCollection services) where TDbContext : IDomainDbContext
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IDomainDbContext>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());
        services.TryAddScoped<IDomainService, EntityFrameworkCoreDomainService>();
    }
}
