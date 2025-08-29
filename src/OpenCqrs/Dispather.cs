using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs;

/// <summary>
/// Provides a unified implementation for dispatching commands, queries, and events in the CQRS pattern.
/// Delegates operations to specialized services for command sending, query processing, and event publishing.
/// </summary>
/// <example>
/// <code>
/// var dispatcher = new Dispatcher(commandSender, queryProcessor, publisher);
/// var result = await dispatcher.Send(new CreateUserCommand { Email = "user@example.com" });
/// </code>
/// </example>
public class Dispatcher(ICommandSender commandSender, IQueryProcessor queryProcessor, IPublisher publisher) : IDispatcher
{
    /// <summary>
    /// Sends a command that does not expect a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TRequest">The type of command to send.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    public async Task<Result> Send<TRequest>(TRequest command, bool validateCommand = false, CancellationToken cancellationToken = default) where TRequest : ICommand
    {
        return await commandSender.Send(command, validateCommand, cancellationToken);
    }

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default)
    {
        return await commandSender.Send(command, validateCommand, cancellationToken);
    }

    /// <summary>
    /// Sends a command for processing, publishes any associated notifications,
    /// and returns the combined results.
    /// </summary>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SendAndPublishResponse"/> containing the result of the command processing
    /// and the results of the published notifications.</returns
    public async Task<SendAndPublishResponse> SendAndPublish(ICommand<CommandResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default)
    {
        return await commandSender.SendAndPublish(command, validateCommand, cancellationToken);
    }

    /// <summary>
    /// Sends a sequence of commands to their corresponding handlers for processing and returns the results for each command.
    /// </summary>
    /// <param name="command">The command sequence to be processed.</param>
    /// <param name="validateCommands">A boolean indicating whether the commands within the sequence should be validated before processing.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task representing an asynchronous operation that returns a collection of <see cref="Result{TValue}"/> objects for each command processed.</returns>
    /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
    public Task<IEnumerable<Result<object>>> Send(ICommandSequence command, bool validateCommands = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Executes a query and returns the commanded data.
    /// </summary>
    /// <typeparam name="TResult">The type of data expected from the query.</typeparam>
    /// <param name="query">The query instance to be executed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the query result on success or failure information.</returns>
    public async Task<Result<TResult>> Get<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        return await queryProcessor.Get(query, cancellationToken);
    }

    /// <summary>
    /// Publishes an notification to all registered handlers that can process the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
    /// <param name="notification">The notification instance to be published.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="Result"/> objects indicating the success or failure of each notification handler.</returns>
    public async Task<IEnumerable<Result>> Publish<TNotification>(INotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        return await publisher.Publish(notification, cancellationToken);
    }
}
