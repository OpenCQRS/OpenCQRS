using OpenCqrs.Results;

namespace OpenCqrs.Events;

/// <summary>
/// Defines a service for publishing domain events to their corresponding event handlers.
/// Events are dispatched to all registered handlers that can process the specific event type.
/// </summary>
/// <example>
/// <code>
/// var results = await publisher.Publish(new UserCreatedEvent 
/// { 
///     UserId = userId, 
///     Email = "user@example.com" 
/// });
/// </code>
/// </example>
public interface IPublisher
{
    /// <summary>
    /// Publishes an event to all registered handlers that can process the specified event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="event">The event instance to be published.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="Result"/> objects indicating the success or failure of each event handler.</returns>
    Task<IEnumerable<Result>> Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
