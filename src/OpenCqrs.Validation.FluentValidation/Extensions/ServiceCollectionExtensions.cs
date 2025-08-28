using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace OpenCqrs.Validation.FluentValidation.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrsFluentValidation(this IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IValidationProvider, FluentValidationProvider>();

        var typeList = types.ToList();

        services.Scan(s => s
            .FromAssembliesOf(typeList)
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
    }
}
