using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation;

public interface IValidationService
{
    Task<Result> Validate<TCommand>(TCommand command) where TCommand : ICommand;
    Task<Result<TResponse>> Validate<TResponse>(ICommand<TResponse> command);
}
