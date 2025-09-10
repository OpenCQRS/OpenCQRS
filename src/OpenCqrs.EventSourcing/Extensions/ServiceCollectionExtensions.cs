using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure event sourcing.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures event sourcing by scanning assemblies for domain events and aggregates.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="types">The types to scan.</param>
    /// <exception cref="ArgumentNullException">Thrown when services or types is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when duplicate type binding keys are encountered.</exception>
    public static void AddOpenCqrsEventSourcing(this IServiceCollection services, params Type[] types)
    {
        var domainEventTypeBindings = new Dictionary<string, Type>();
        var aggregateTypeBindings = new Dictionary<string, Type>();

        foreach (var type in types)
        {
            var assembly = type.Assembly;

            var domainEvents = assembly.GetTypes()
                .Where(t => t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract && typeof(IDomainEvent).IsAssignableFrom(t))
                .ToList();

            var aggregates = assembly.GetTypes()
                .Where(t => t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(t))
                .ToList();

            foreach (var domainEvent in domainEvents)
            {
                var domainEventType = domainEvent.GetCustomAttribute<DomainEventType>();
                if (domainEventType is null)
                {
                    continue;
                }
                domainEventTypeBindings.Add(TypeBindings.GetTypeBindingKey(domainEventType.Name, domainEventType.Version), domainEvent);
            }

            foreach (var aggregate in aggregates)
            {
                var aggregateType = aggregate.GetCustomAttribute<AggregateType>();
                if (aggregateType is null)
                {
                    continue;
                }
                aggregateTypeBindings.Add(TypeBindings.GetTypeBindingKey(aggregateType.Name, aggregateType.Version), aggregate);
            }
        }

        TypeBindings.DomainEventTypeBindings = domainEventTypeBindings;
        TypeBindings.AggregateTypeBindings = aggregateTypeBindings;
    }
}
