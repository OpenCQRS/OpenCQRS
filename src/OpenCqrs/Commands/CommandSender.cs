using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Notifications;
using OpenCqrs.Results;

namespace OpenCqrs.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandSender"/> that dispatches commands to their
/// corresponding handlers using dependency injection. Provides caching for improved performance
/// when handling commands that return responses.
/// </summary>
/// <example>
/// <code>
/// // Use in controller
/// var result = await commandSender.Send(new CreateUserCommand 
/// { 
///     FirstName = "John", 
///     LastName = "Doe" 
/// });
/// </code>
/// </example>
public class CommandSender(IServiceProvider serviceProvider, IPublisher publisher) : ICommandSender
{
    private static readonly ConcurrentDictionary<Type, object?> CommandHandlerWrappers = new();

    /// <summary>
    /// Sends a command that does not expect a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="Exception">Thrown when no handler is found for the command type.</exception>
    public async Task<Result> Send<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>();

        if (handler is null)
        {
            throw new Exception("Command handler not found.");
        }

        return await handler.Handle(command, cancellationToken);
    }

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();

        var handler = (CommandHandlerWrapperBase<TResponse>)CommandHandlerWrappers.GetOrAdd(commandType, _ =>
            Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(commandType, typeof(TResponse))))!;

        var result = await handler.Handle(command, serviceProvider, cancellationToken);

        return result;
    }

    /// <summary>
    /// Sends a command to its corresponding handler for processing and publishes any resulting notifications.
    /// </summary>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SendAndPublishResponse"/> containing the result of the command processing and the results of all published notifications.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
    /// <exception cref="Exception">Thrown when no appropriate handler is found for the command type.</exception>
    public async Task<SendAndPublishResponse> SendAndPublish(ICommand<CommandResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();

        var handler = (CommandHandlerWrapperBase<CommandResponse>)CommandHandlerWrappers.GetOrAdd(commandType, _ =>
            Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(commandType, typeof(CommandResponse))))!;

        var commandResult = await handler.Handle(command, serviceProvider, cancellationToken);

        if (commandResult.IsNotSuccess
            || commandResult.Value?.Notifications == null
            || !commandResult.Value?.Notifications.Any() is false)
        {
            return new SendAndPublishResponse(commandResult, NotificationResults: []);
        }

        var tasks = commandResult.Value!.Notifications
            .Select(notification => publisher.Publish(notification, cancellationToken))
            .ToList();

        var notificationsResults = await Task.WhenAll(tasks);

        return new SendAndPublishResponse(commandResult, notificationsResults.SelectMany(r => r).ToList());
    }
}
