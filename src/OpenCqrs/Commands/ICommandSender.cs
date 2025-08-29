using OpenCqrs.Results;

namespace OpenCqrs.Commands;

/// <summary>
/// Defines a service for dispatching commands to their corresponding handlers in the CQRS pattern.
/// Provides methods for sending both commands without responses and commands that return values.
/// </summary>
/// <example>
/// <code>
/// var result = await commandSender.Send(new CreateUserCommand 
/// { 
///     FirstName = "John", 
///     LastName = "Doe" 
/// });
/// </code>
/// </example>
public interface ICommandSender
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
    /// Sends a command that expects a <see cref="CommandResponse"/> as a result to its handler for processing
    /// and subsequently publishes related notifications associated with the command.
    /// </summary>
    /// <param name="command">The command instance to be processed and published.</param>
    /// <param name="validateCommand"></param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="SendAndPublishResponse"/> containing the command processing result
    /// and a collection of results from the published notifications.</returns>
    Task<SendAndPublishResponse> SendAndPublish(ICommand<CommandResponse> command, bool validateCommand = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a sequence of commands to their corresponding handlers for processing.
    /// </summary>
    /// <param name="command">The command sequence to be processed.</param>
    /// <param name="validateCommands">A boolean indicating whether the commands in the sequence should be validated before processing.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an enumerable of <see cref="Result{object}"/> that represent the outcome of each processed command in the sequence.</returns>
    Task<IEnumerable<Result<object>>> Send(ICommandSequence command, bool validateCommands = false, CancellationToken cancellationToken = default);
}
