namespace OpenCqrs.Events;

/// <summary>
/// Defines an event in the CQRS pattern that represents something significant that has occurred
/// in the domain. Events are immutable notifications that describe state changes and are
/// typically handled by one or more event handlers.
/// </summary>
/// <example>
/// <code>
/// public record UserCreatedEvent : IEvent
/// {
///     public Guid UserId { get; init; }
///     public string Email { get; init; } = string.Empty;
///     public DateTime CreatedAt { get; init; }
/// }
/// </code>
/// </example>
public interface IEvent;
