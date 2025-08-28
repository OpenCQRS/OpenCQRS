using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;

namespace OpenCqrs.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrs(this IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<ICommandSender, CommandSender>();
        services.AddScoped<IQueryProcessor, QueryProcessor>();
        services.AddScoped<IPublisher, Publisher>();

        var typeList = types.ToList();

        services.Scan(s => s
            .FromAssembliesOf(typeList)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
    }
}
