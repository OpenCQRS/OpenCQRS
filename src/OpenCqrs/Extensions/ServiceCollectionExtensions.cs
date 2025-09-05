using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Caching;
using OpenCqrs.Commands;
using OpenCqrs.Messaging;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Validation;

namespace OpenCqrs.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrs(this IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<ICommandSender, CommandSender>();
        services.AddScoped<IQueryProcessor, QueryProcessor>();
        services.AddScoped<ICachingService, CachingService>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<IMessagePublisher, MessagePublisher>();
        services.AddScoped<IValidationService, ValidationService>();

        services.AddScoped<ICachingProvider, DefaultCachingProvider>();
        services.AddScoped<IMessagingProvider, DefaultMessagingProvider>();
        services.AddScoped<IValidationProvider, DefaultValidationProvider>();

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
                .WithScopedLifetime());
    }
}
