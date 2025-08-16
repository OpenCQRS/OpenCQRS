using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Domain;
using OpenCqrs.Notifications;
using OpenCqrs.Requests;
using OpenCqrs.Streams;
// using AutoMapper;

namespace OpenCqrs.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddOpenCqrs(this IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IPublisher, Publisher>();
        services.AddScoped<IRequestSender, RequestSender>();
        // services.AddScoped<ISlugGenerator, SlugGenerator>();
        services.AddScoped<IStreamCreator, StreamCreator>();
        
        var typeList = types.ToList();
        
        services.Scan(s => s
            .FromAssembliesOf(typeList)
            // .AddClasses(classes => classes.AssignableTo(typeof(IDomainEvent)))
            //     .AsImplementedInterfaces()
            //     .WithSingletonLifetime()
            // .AddClasses(classes => classes.AssignableTo(typeof(IViewKey<>)))
            //     .AsImplementedInterfaces()
            //     .WithSingletonLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IStreamRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        // services.AddAutoMapper(typeList);
        services.AddTypeBindings(typeList);
    }
    
    // private static void AddAutoMapper(this IServiceCollection services, IEnumerable<Type> types)
    // {
    //     var mapperConfiguration = new MapperConfiguration(configuration =>
    //     {
    //         configuration.ShouldMapMethod = _ => false;
    //         foreach (var type in types)
    //         {
    //             var typesToMap = type.Assembly.GetTypes()
    //                 .Where(t =>
    //                     t.GetTypeInfo().IsClass &&
    //                     !t.GetTypeInfo().IsAbstract &&
    //                     typeof(INotification).IsAssignableFrom(t))
    //                 .ToList();
    //
    //             foreach (var typeToMap in typesToMap)
    //             {
    //                 configuration.CreateMap(typeToMap, typeToMap);
    //             }
    //         }
    //     });
    //
    //     services.AddSingleton(_ => mapperConfiguration.CreateMapper());
    // }

    private static void AddTypeBindings(this IServiceCollection services, IEnumerable<Type> types)
    {
        var domainEventBindings = new Dictionary<string, Type>();
        var streamViewBindings = new Dictionary<string, Type>();
        
        foreach (var type in types)
        {
            var assembly = type.Assembly;
            
            var domainEvents = assembly.GetImplementationsOf<IDomainEvent>();
            var streamViews = assembly.GetImplementationsOf<IStreamView>();
            
            foreach (var domainEvent in domainEvents)
            {
                var domainEventType = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
                if (domainEventType is null)
                {
                    continue;
                }
                domainEventBindings.Add(TypeBindings.GetTypeBindingKey(domainEventType.Name, domainEventType.Version), domainEvent.GetType());
            }
            
            foreach (var streamView in streamViews)
            {
                var streamViewType = streamView.GetType().GetCustomAttribute<StreamViewType>();
                if (streamViewType is null)
                {
                    continue;
                }
                streamViewBindings.Add(TypeBindings.GetTypeBindingKey(streamViewType.Name, streamViewType.Version), streamView.GetType());
            }
        }
        
        TypeBindings.DomainEventBindings = domainEventBindings;
        TypeBindings.StreamViewBindings = streamViewBindings;
    }
}
