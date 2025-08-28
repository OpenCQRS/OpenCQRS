using OpenCqrs.Commands;

namespace OpenCqrs.Validation;

public interface IValidationProvider
{
    Task<ValidationResponse> Validate<TCommand>(TCommand command) where TCommand : ICommand;
}
