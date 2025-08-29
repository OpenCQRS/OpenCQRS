using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Notifications;
using OpenCqrs.Results;
using OpenCqrs.Validation;

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
public class CommandSender(IServiceProvider serviceProvider, IValidationService validationService, IPublisher publisher) : ICommandSender
{
    private static readonly ConcurrentDictionary<Type, object?> CommandHandlerWrappers = new();

    private static readonly ConcurrentDictionary<Type, Func<IPublisher, INotification, CancellationToken, Task<IEnumerable<Result>>>> CompiledPublishers = new();

    /// <summary>
    /// Sends a command that does not expect a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the command processing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="Exception">Thrown when no handler is found for the command type.</exception>
    public async Task<Result> Send<TCommand>(TCommand command, bool validateCommand = false, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validateCommand)
        {
            var validationResult = await validationService.Validate(command);
            if (validationResult.IsNotSuccess)
            {
                return validationResult;
            }
        }

        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
        {
            throw new Exception($"Command handler for {typeof(TCommand).Name} not found.");
        }

        return await handler.Handle(command, cancellationToken);
    }

    /// <summary>
    /// Sends a command that expects a response value to its corresponding handler for processing.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the command.</typeparam>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the response value on success or failure information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validateCommand)
        {
            var validationResult = await validationService.Validate(command);
            if (validationResult.IsNotSuccess)
            {
                return validationResult;
            }
        }

        var commandType = command.GetType();

        var handler = (CommandHandlerWrapperBase<TResponse>)CommandHandlerWrappers.GetOrAdd(commandType, _ =>
            Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(commandType, typeof(TResponse))))!;

        if (handler is null)
        {
            throw new Exception($"Command handler for {typeof(ICommand<TResponse>).Name} not found.");
        }

        var result = await handler.Handle(command, serviceProvider, cancellationToken);

        return result;
    }

    /// <summary>
    /// Sends a command to its corresponding handler for processing and publishes any resulting notifications.
    /// </summary>
    /// <param name="command">The command instance to be processed.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SendAndPublishResponse"/> containing the result of the command processing and the results of all published notifications.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
    /// <exception cref="Exception">Thrown when no appropriate handler is found for the command type.</exception>
    public async Task<SendAndPublishResponse> SendAndPublish(ICommand<CommandResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validateCommand)
        {
            var validationResult = await validationService.Validate(command);
            if (validationResult.IsNotSuccess)
            {
                return new SendAndPublishResponse(CommandResult: validationResult, NotificationResults: []);
            }
        }

        var commandType = command.GetType();

        var handler = (CommandHandlerWrapperBase<CommandResponse>)CommandHandlerWrappers.GetOrAdd(commandType, _ =>
            Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(commandType, typeof(CommandResponse))))!;

        if (handler is null)
        {
            throw new Exception($"Command handler for {typeof(ICommand<CommandResponse>).Name} not found.");
        }

        var commandResult = await handler.Handle(command, serviceProvider, cancellationToken);

        if (commandResult.IsNotSuccess
            || commandResult.Value?.Notifications == null
            || commandResult.Value?.Notifications.Any() is false)
        {
            return new SendAndPublishResponse(commandResult, NotificationResults: []);
        }

        var tasks = commandResult.Value!.Notifications
            .Select(notification =>
            {
                var notificationType = notification.GetType();
                var publishDelegate = GetOrCreateCompiledPublisher(notificationType);
                return publishDelegate(publisher, notification, cancellationToken);
            })
            .ToList();

        var notificationsResults = await Task.WhenAll(tasks);

        return new SendAndPublishResponse(commandResult, notificationsResults.SelectMany(r => r).ToList());
    }

    public async Task<IEnumerable<Result<TResponse>>> Send<TResponse>(ICommandSequence<TResponse> commandSequence, bool validateCommands = false, bool stopProcessingOnFirstFailure = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandSequence);

        var commandResults = new List<Result<TResponse>>();

        foreach (var command in commandSequence.Commands)
        {
            if (validateCommands)
            {
                var validationResult = await validationService.Validate(command);
                if (validationResult.IsNotSuccess)
                {
                    commandResults.Add(validationResult);

                    if (stopProcessingOnFirstFailure)
                    {
                        break;
                    }

                    continue;
                }
            }

            var commandType = command.GetType();

            var handler = (CommandSequenceHandlerWrapperBase<TResponse>)CommandHandlerWrappers.GetOrAdd(commandType, _ =>
                Activator.CreateInstance(typeof(CommandSequenceHandlerWrapper<,>).MakeGenericType(commandType, typeof(TResponse))))!;

            if (handler is null)
            {
                throw new Exception($"Command sequence handler for {typeof(ICommand<TResponse>).Name} not found.");
            }

            var commandResult = await handler.Handle(command, commandResults, serviceProvider, cancellationToken);
            commandResults.Add(commandResult);

            if (stopProcessingOnFirstFailure && commandResult.IsNotSuccess)
            {
                break;
            }
        }

        return commandResults;
    }

    private static Func<IPublisher, INotification, CancellationToken, Task<IEnumerable<Result>>> GetOrCreateCompiledPublisher(Type notificationType)
    {
        return CompiledPublishers.GetOrAdd(notificationType, type =>
        {
            var publisherParam = Expression.Parameter(typeof(IPublisher), "publisher");
            var notificationParam = Expression.Parameter(typeof(INotification), "notification");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var publishMethod = typeof(IPublisher).GetMethod(nameof(IPublisher.Publish))!.MakeGenericMethod(type);
            var castNotification = Expression.Convert(notificationParam, type);

            var methodCall = Expression.Call(publisherParam, publishMethod, castNotification, cancellationTokenParam);

            var lambda = Expression.Lambda<Func<IPublisher, INotification, CancellationToken, Task<IEnumerable<Result>>>>(methodCall, publisherParam, notificationParam, cancellationTokenParam);

            return lambda.Compile();
        });
    }
}
