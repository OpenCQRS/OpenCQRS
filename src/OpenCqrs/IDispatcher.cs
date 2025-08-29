using OpenCqrs.Commands;
using OpenCqrs.Notifications;
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
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    Task<Result> Send<TCommand>(TCommand command, bool validateCommand = false, CancellationToken cancellationToken = default) where TCommand : ICommand;

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command for processing and publishes any corresponding notifications.
    /// </summary>
    /// <param name="command">The command instance to be sent for processing.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SendAndPublishResponse"/> containing the result of the command processing and the notification publishing results.</returns>
    Task<SendAndPublishResponse> SendAndPublish(ICommand<CommandResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a sequence of commands to their respective handlers for processing.
    /// </summary>
    /// <param name="command">The command sequence to be processed.</param>
    /// <param name="validateCommands">Indicates whether the commands should be validated before processing.</param>
    /// <param name="stopProcessingOnFirstFailure">When true, stops processing remaining commands in the sequence if any command fails. When false, continues processing all commands regardless of individual failures.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents an asynchronous operation. The task result contains a collection of <see cref="Result{TValue}"/> objects, representing the outcome for each command in the sequence.</returns>
    Task<IEnumerable<Result<TResponse>>> Send<TResponse>(ICommandSequence<TResponse> command, bool validateCommands = false, bool stopProcessingOnFirstFailure = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns the requested data.
    /// </summary>
    /// <typeparam name="TResult">The type of data expected from the query.</typeparam>
    /// <param name="query">The query instance to be executed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the query result on success or failure information.</returns>
    Task<Result<TResult>> Get<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Result>> Publish<TNotification>(INotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}
