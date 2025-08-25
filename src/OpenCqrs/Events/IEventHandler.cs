using OpenCqrs.Results;

namespace OpenCqrs.Events;

/// <summary>
/// Defines a handler for processing domain events in the CQRS pattern.
/// Event handlers contain the logic for responding to events and performing
/// side effects or additional business operations.
/// </summary>
/// <typeparam name="TEvent">The type of event this handler processes.</typeparam>
/// <example>
/// <code>
/// public class UserCreatedEventHandler : IEventHandler&lt;UserCreatedEvent&gt;
/// {
///     public async Task&lt;Result&gt; Handle(UserCreatedEvent @event, CancellationToken cancellationToken)
///     {
///         // Send welcome email or perform other side effects
///         return Result.Ok();
///     }
/// }
/// </code>
/// </example>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the specified event and performs any necessary business logic or side effects.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the event processing.</returns>
    Task<Result> Handle(TEvent @event, CancellationToken cancellationToken = default);
}
