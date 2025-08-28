using OpenCqrs.Commands;

namespace OpenCqrs.Validation;

public interface IValidationProvider
{
    Task<ValidationResponse> Validate<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
    Task<ValidationResponse> Validate<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
}
