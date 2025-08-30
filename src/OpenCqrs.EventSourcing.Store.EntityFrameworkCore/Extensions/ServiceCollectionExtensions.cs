using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsEntityFrameworkCore(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainService, EntityFrameworkCoreDomainService>();
    }
}
