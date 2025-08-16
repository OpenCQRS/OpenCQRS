using FluentValidation.Results;

namespace OpenCqrs.Extensions;

public static class FluentValidationExtensions
{
    public static string ToErrorMessage(this IEnumerable<ValidationFailure> validationFailures)
    {
        var errorMessages = validationFailures.Select(validationFailure => validationFailure.ErrorMessage).ToArray();
        return $"Errors: {string.Join("; ", errorMessages)}";
    }
}
