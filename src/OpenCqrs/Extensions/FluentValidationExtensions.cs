using FluentValidation.Results;

namespace OpenCqrs.Extensions;

/// <summary>
/// Extension methods for FluentValidation types to provide convenient formatting and conversion utilities.
/// Simplifies working with validation results in CQRS command and query handlers by providing
/// standardized error message formatting.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Converts a collection of validation failures into a formatted error message string.
    /// </summary>
    /// <param name="validationFailures">The collection of validation failures to format. Each failure contains details about what validation rule failed and why.</param>
    /// <returns>
    /// A formatted string containing all error messages from the validation failures, prefixed with "Errors:" and
    /// separated by semicolons. For example, "Errors: Name is required; Age must be greater than 0".
    /// Returns "Errors:" if the collection is empty.
    /// </returns>
    /// <example>
    /// <code>
    /// var failures = new List&lt;ValidationFailure&gt;
    /// {
    ///     new ValidationFailure("Name", "Name is required"),
    ///     new ValidationFailure("Age", "Age must be greater than 0")
    /// };
    /// 
    /// string errorMessage = failures.ToErrorMessage();
    /// // Result: "Errors: Name is required; Age must be greater than 0"
    /// </code>
    /// </example>
    public static string ToErrorMessage(this IEnumerable<ValidationFailure> validationFailures)
    {
        var errorMessages = validationFailures.Select(validationFailure => validationFailure.ErrorMessage).ToArray();
        return $"Errors: {string.Join("; ", errorMessages)}";
    }
}
