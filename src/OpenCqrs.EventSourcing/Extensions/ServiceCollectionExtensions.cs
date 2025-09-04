using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to configure event sourcing infrastructure
/// and register domain types for serialization and deserialization in the OpenCqrs event sourcing system.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures event sourcing infrastructure by scanning the provided assemblies for domain events and aggregates,
    /// then registering their type bindings for proper serialization and deserialization.
    /// </summary>
    /// <param name="services">The service collection to configure (though no services are directly registered by this method).</param>
    /// <param name="types">
    /// A collection of types whose assemblies will be scanned for domain events and aggregates.
    /// Each unique assembly will be processed to discover event sourcing types.
    /// </param>
    /// <example>
    /// <code>
    /// // Basic registration with marker types
    /// services.AddOpenCqrsEventSourcing(new[]
    /// {
    ///     typeof(OrderAggregate),      // Assembly containing order domain
    ///     typeof(CustomerAggregate),   // Assembly containing customer domain  
    ///     typeof(PaymentAggregate)     // Assembly containing payment domain
    /// });
    /// 
    /// // Registration in Startup.cs or Program.cs
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     // Register other services...
    ///     services.AddDbContext&lt;EventStoreContext&gt;(options =&gt; ...);
    ///     
    ///     // Configure event sourcing - this should be called after DbContext registration
    ///     // but before registering repositories or other event sourcing services
    ///     services.AddOpenCqrsEventSourcing(new[]
    ///     {
    ///         typeof(DomainAssemblyMarker), // Marker type from domain assembly
    ///         typeof(AnotherDomainMarker)   // Marker type from another domain assembly
    ///     });
    ///     
    ///     // Register event sourcing repositories and services...
    ///     services.AddScoped&lt;IAggregateRepository, AggregateRepository&gt;();
    /// }
    /// 
    /// // Example domain event with proper attribute
    /// [DomainEventType("OrderPlaced", 1)]
    /// public record OrderPlacedEvent : IDomainEvent
    /// {
    ///     public Guid OrderId { get; init; }
    ///     public DateTime PlacedAt { get; init; }
    /// }
    /// 
    /// // Example aggregate with proper attribute  
    /// [AggregateType("Order", 1)]
    /// public class OrderAggregate : Aggregate
    /// {
    ///     // Aggregate implementation...
    /// }
    /// 
    /// // Verification - check registered types
    /// public void VerifyRegistration()
    /// {
    ///     var hasOrderEvent = TypeBindings.DomainEventTypeBindings.ContainsKey("OrderPlaced_v1");
    ///     var hasOrderAggregate = TypeBindings.AggregateTypeBindings.ContainsKey("Order_v1");
    ///     
    ///     if (hasOrderEvent && hasOrderAggregate)
    ///     {
    ///         Console.WriteLine("Event sourcing types registered successfully");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="types"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when duplicate type binding keys are encountered (same name and version).</exception>
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
                .Where(t => t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract && typeof(IAggregate).IsAssignableFrom(t))
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
