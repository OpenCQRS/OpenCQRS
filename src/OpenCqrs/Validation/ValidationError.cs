namespace OpenCqrs.Validation;

/// <summary>
/// Represents a validation error that contains details about a specific property and its related error message.
/// </summary>
public class ValidationError
{
    public required string PropertyName { get; set; }
    public required string ErrorMessage { get; set; }
}
