using OpenCqrs.Commands;
using OpenCqrs.Events;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs;

/// <summary>
/// Provides a unified interface for dispatching commands, queries, and events in the CQRS pattern.
/// Acts as a centralized entry point for all CQRS operations within the application.
/// </summary>
/// <example>
/// <code>
/// var result = await dispatcher.Send(new CreateUserCommand { Email = "user@example.com" });
/// </code>
/// </example>
public interface IDispatcher
{
    /// <summary>
    /// Sends a command that does not expect a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    Task<Result> Send<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns the requested data.
    /// </summary>
    /// <typeparam name="TResult">The type of data expected from the query.</typeparam>
    /// <param name="query">The query instance to be executed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the query result on success or failure information.</returns>
    Task<Result<TResult>> Get<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an event to all registered handlers that can process the specified event type.
    /// </summary>
    /// <typeparam name="TNotification">The type of event to publish.</typeparam>
    /// <param name="event">The event instance to be published.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="Result"/> objects indicating the success or failure of each event handler.</returns>
    Task<IEnumerable<Result>> Publish<TNotification>(IEvent @event, CancellationToken cancellationToken = default) where TNotification : IEvent;
}
