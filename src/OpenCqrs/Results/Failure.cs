using FluentValidation.Results;
using OpenCqrs.Extensions;

namespace OpenCqrs.Results;

public record Failure(ErrorCode ErrorCode = ErrorCode.Error, string? Title = null, string? Description = null, string? Type = null, IDictionary<string, string>? Tags = null);

public static class FailureExtensions
{
    public static Failure WithTitle(this Failure failure, string title) => 
        failure with { Title = title };

    public static Failure WithDescription(this Failure failure, string description) => 
        failure with { Description = description };

    public static Failure WithDescription(this Failure failure, string description, IEnumerable<string> items) => 
        failure with { Description = $"{description}: {string.Join(", ", items)}" };

    public static Failure WithDescription(this Failure failure, ValidationResult validationResult) => 
        failure with { Description = validationResult.Errors.ToErrorMessage() };

    public static Failure WithType(this Failure failure, string type) =>
        failure with { Type = type };
    
    public static Failure WithTags(this Failure failure, IDictionary<string, string> tags) =>
        failure with { Tags = tags };
}

public enum ErrorCode
{
    Error,
    NotFound,
    Unauthorized,
    UnprocessableEntity,
    BadRequest
}
