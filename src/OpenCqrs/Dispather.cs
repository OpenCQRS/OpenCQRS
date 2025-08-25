using OpenCqrs.Commands;
using OpenCqrs.Events;
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
    /// <param name="request">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    public async Task<Result> Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : ICommand
    {
        return await commandSender.Send(request, cancellationToken);
    }

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="request">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> request, CancellationToken cancellationToken = default)
    {
        return await commandSender.Send(request, cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns the requested data.
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
    /// Publishes an event to all registered handlers that can process the specified event type.
    /// </summary>
    /// <typeparam name="TNotification">The type of event to publish.</typeparam>
    /// <param name="event">The event instance to be published.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of <see cref="Result"/> objects indicating the success or failure of each event handler.</returns>
    public async Task<IEnumerable<Result>> Publish<TNotification>(IEvent @event, CancellationToken cancellationToken = default) where TNotification : IEvent
    {
        return await publisher.Publish(@event, cancellationToken);
    }
}
