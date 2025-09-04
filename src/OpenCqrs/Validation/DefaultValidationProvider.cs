using OpenCqrs.Commands;

namespace OpenCqrs.Validation;

public class DefaultValidationProvider : IValidationProvider
{
    public Task<ValidationResponse> Validate<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        throw new NotImplementedException();
    }

    public Task<ValidationResponse> Validate<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
