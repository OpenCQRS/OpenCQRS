using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Results;

namespace OpenCqrs.Events;

/// <summary>
/// Provides event publishing functionality by dispatching events to all registered event handlers.
/// Resolves handlers from the service provider and executes them concurrently.
/// </summary>
/// <example>
/// <code>
/// var publisher = new Publisher(serviceProvider);
/// var results = await publisher.Publish(new UserCreatedEvent { UserId = userId });
/// </code>
/// </example>
public class Publisher(IServiceProvider serviceProvider) : IPublisher
{
    /// <summary>
    /// Publishes an event to all registered handlers that can process the specified event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="event">The event instance to be published.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="Result"/> objects indicating the success or failure of each event handler.</returns>
    public async Task<IEnumerable<Result>> Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();

        var eventHandlers = handlers as IEventHandler<TEvent>[] ?? handlers.ToArray();
        if (eventHandlers.Length == 0)
        {
            return [];
        }

        var tasks = eventHandlers.Select(handler => handler.Handle(@event, cancellationToken)).ToList();

        return await Task.WhenAll(tasks);
    }
}
