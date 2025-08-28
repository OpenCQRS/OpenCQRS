using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation;

public class ValidationService(IValidationProvider validationProvider) : IValidationService
{
    public async Task<Result> Validate<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var validationResponse = await validationProvider.Validate(command);

        return !validationResponse.IsValid 
            ? Result.Fail(ErrorCode.BadRequest, title: "Validation Failed", description: BuildErrorMessage(validationResponse.Errors)) 
            : Result.Ok();
    }

    private static string BuildErrorMessage(IEnumerable<ValidationError> validationErrors)
    {
        var errorsText = validationErrors.Select(ve => $"\r\n - {ve.ErrorMessage}").ToArray();
        return $"Validation failed: {string.Join("", errorsText)}";
    }
}
