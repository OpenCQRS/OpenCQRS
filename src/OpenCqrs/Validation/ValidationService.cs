using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation;

public class ValidationService(IValidationProvider validationProvider) : IValidationService
{
    public async Task<Result> Validate<TCommand>(TCommand command) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationResponse = await validationProvider.Validate(command);

        return !validationResponse.IsValid
            ? Result.Fail(ErrorCode.BadRequest, title: "Validation Failed", description: BuildErrorMessage(validationResponse.Errors))
            : Result.Ok();
    }

    public async Task<Result<TResponse>> Validate<TResponse>(ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationResponse = await validationProvider.Validate(command);

        return !validationResponse.IsValid
            ? Result<TResponse>.Fail(ErrorCode.BadRequest, title: "Validation Failed", description: BuildErrorMessage(validationResponse.Errors))
            : Result<TResponse>.Ok(new Success<TResponse>());
    }

    private static string BuildErrorMessage(IEnumerable<ValidationError> validationErrors)
    {
        var errorMessages = validationErrors.Select(ve => ve.ErrorMessage).ToArray();
        return $"Validation failed with errors: {string.Join("; ", errorMessages)}";
    }
}
