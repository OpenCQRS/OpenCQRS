namespace OpenCqrs.Results;

/// <summary>
/// Represents a failed operation result containing detailed error information.
/// Used in the Result pattern to encapsulate failure states with comprehensive error details
/// for proper error handling and user feedback.
/// </summary>
/// <param name="ErrorCode">
/// The error code categorizing the type of failure. Defaults to <see cref="OpenCqrs.Results.ErrorCode.Error"/>.
/// </param>
/// <param name="Title">
/// Optional brief title describing the error. Used for user-facing error messages.
/// </param>
/// <param name="Description">
/// Optional detailed description of the error. Provides context about what went wrong and potentially how to fix it.
/// </param>
/// <param name="Type">
/// Optional error type classification for categorizing different kinds of errors within the same error code.
/// </param>
/// <param name="Tags">
/// Optional dictionary of additional metadata associated with the error. Useful for logging, monitoring, and debugging.
/// </param>
/// <example>
/// <code>
/// // Simple error with just an error code
/// var simpleFailure = new Failure(ErrorCode.NotFound);
/// 
/// // Comprehensive error with all details
/// var detailedFailure = new Failure(
///     ErrorCode.BadRequest,
///     "Invalid User Data",
///     "The provided email address is not valid. Please provide a valid email format.",
///     "validation.email",
///     new Dictionary&lt;string, string&gt;
///     {
///         ["field"] = "email",
///         ["providedValue"] = "invalid-email",
///         ["validationRule"] = "email-format"
///     }
/// );
/// 
/// // Using in business logic
/// public Result&lt;User&gt; CreateUser(string email, string name)
/// {
///     if (string.IsNullOrEmpty(email))
///         return new Failure(ErrorCode.BadRequest, "Missing Email", "Email address is required");
///     
///     if (!IsValidEmail(email))
///         return new Failure(ErrorCode.BadRequest, "Invalid Email", $"'{email}' is not a valid email format");
///     
///     if (UserExists(email))
///         return new Failure(ErrorCode.UnprocessableEntity, "User Exists", "A user with this email already exists");
///     
///     // Success path...
///     return Result&lt;User&gt;.Ok(new User(email, name));
/// }
/// 
/// // Handling failures
/// var result = CreateUser("invalid-email", "John");
/// if (result.IsFailure)
/// {
///     var failure = result.Failure;
///     logger.LogError("User creation failed: {Title} - {Description}", failure.Title, failure.Description);
///     
///     // Map to HTTP status codes
///     var statusCode = failure.ErrorCode switch
///     {
///         ErrorCode.BadRequest => 400,
///         ErrorCode.NotFound => 404,
///         ErrorCode.UnprocessableEntity => 422,
///         _ => 500
///     };
/// }
/// </code>
/// </example>
public record Failure(ErrorCode ErrorCode = ErrorCode.Error, string? Title = null, string? Description = null, string? Type = null, IDictionary<string, string>? Tags = null);

/// <summary>
/// Provides extension methods for the <see cref="Failure"/> record to enable fluent configuration
/// and easy modification of failure instances using record with-expressions.
/// </summary>
/// <example>
/// <code>
/// // Fluent error construction
/// var failure = new Failure(ErrorCode.BadRequest)
///     .WithTitle("Validation Failed")
///     .WithDescription("The request contains invalid data")
///     .WithType("validation.request")
///     .WithTags(new Dictionary&lt;string, string&gt; { ["source"] = "user-input" });
/// 
/// // Building errors progressively
/// var baseFailure = new Failure(ErrorCode.UnprocessableEntity);
/// 
/// var validationFailure = baseFailure
///     .WithTitle("User Validation Failed")
///     .WithDescription(validationResult);
/// 
/// var enrichedFailure = validationFailure
///     .WithTags(new Dictionary&lt;string, string&gt; { ["userId"] = userId.ToString() });
/// </code>
/// </example>
public static class FailureExtensions
{
    /// <summary>
    /// Returns a new failure instance with the specified title.
    /// </summary>
    /// <param name="failure">The failure instance to modify.</param>
    /// <param name="title">The title to set for the error.</param>
    /// <returns>A new <see cref="Failure"/> instance with the updated title.</returns>
    /// <example>
    /// <code>
    /// var failure = new Failure(ErrorCode.BadRequest).WithTitle("Invalid Input");
    /// </code>
    /// </example>
    public static Failure WithTitle(this Failure failure, string title) =>
        failure with { Title = title };

    /// <summary>
    /// Returns a new failure instance with the specified description.
    /// </summary>
    /// <param name="failure">The failure instance to modify.</param>
    /// <param name="description">The detailed description to set for the error.</param>
    /// <returns>A new <see cref="Failure"/> instance with the updated description.</returns>
    /// <example>
    /// <code>
    /// var failure = new Failure(ErrorCode.NotFound)
    ///     .WithDescription("The user with ID 12345 could not be found in the database");
    /// </code>
    /// </example>
    public static Failure WithDescription(this Failure failure, string description) =>
        failure with { Description = description };

    /// <summary>
    /// Returns a new failure instance with a description that combines the provided description
    /// with a comma-separated list of items.
    /// </summary>
    /// <param name="failure">The failure instance to modify.</param>
    /// <param name="description">The base description text.</param>
    /// <param name="items">The items to append to the description.</param>
    /// <returns>A new <see cref="Failure"/> instance with the combined description.</returns>
    /// <example>
    /// <code>
    /// var missingFields = new[] { "Email", "Name", "Phone" };
    /// var failure = new Failure(ErrorCode.BadRequest)
    ///     .WithDescription("Missing required fields", missingFields);
    /// // Results in: "Missing required fields: Email, Name, Phone"
    /// </code>
    /// </example>
    public static Failure WithDescription(this Failure failure, string description, IEnumerable<string> items) =>
        failure with { Description = $"{description}: {string.Join(", ", items)}" };

    /// <summary>
    /// Returns a new failure instance with the specified error type.
    /// </summary>
    /// <param name="failure">The failure instance to modify.</param>
    /// <param name="type">The error type classification to set.</param>
    /// <returns>A new <see cref="Failure"/> instance with the updated type.</returns>
    /// <example>
    /// <code>
    /// // Different types of validation errors
    /// var emailError = new Failure(ErrorCode.BadRequest).WithType("validation.email");
    /// var phoneError = new Failure(ErrorCode.BadRequest).WithType("validation.phone");
    /// var formatError = new Failure(ErrorCode.BadRequest).WithType("validation.format");
    /// </code>
    /// </example>
    public static Failure WithType(this Failure failure, string type) =>
        failure with { Type = type };

    /// <summary>
    /// Returns a new failure instance with the specified metadata tags.
    /// </summary>
    /// <param name="failure">The failure instance to modify.</param>
    /// <param name="tags">The dictionary of metadata tags to associate with the error.</param>
    /// <returns>A new <see cref="Failure"/> instance with the updated tags.</returns>
    /// <example>
    /// <code>
    /// var failure = new Failure(ErrorCode.BadRequest)
    ///     .WithTags(new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["userId"] = "12345",
    ///         ["operation"] = "create-user",
    ///         ["source"] = "user-registration-form",
    ///         ["correlationId"] = Guid.NewGuid().ToString()
    ///     });
    /// </code>
    /// </example>
    public static Failure WithTags(this Failure failure, IDictionary<string, string> tags) =>
        failure with { Tags = tags };
}

/// <summary>
/// Defines standardized error codes for categorizing different types of failures in the application.
/// These codes align with common HTTP status code patterns but can be used in any context where
/// error categorization is needed.
/// </summary>
/// <example>
/// <code>
/// // Using error codes in business logic
/// public Result&lt;User&gt; GetUser(Guid userId)
/// {
///     if (userId == Guid.Empty)
///         return Result&lt;User&gt;.Fail(ErrorCode.BadRequest, "Invalid user ID");
///     
///     var user = _repository.GetById(userId);
///     if (user == null)
///         return Result&lt;User&gt;.Fail(ErrorCode.NotFound, "User not found");
///     
///     if (!_authService.CanAccessUser(user))
///         return Result&lt;User&gt;.Fail(ErrorCode.Unauthorized, "Access denied");
///     
///     return Result&lt;User&gt;.Ok(user);
/// }
/// 
/// // Converting to HTTP status codes
/// public IActionResult HandleResult&lt;T&gt;(Result&lt;T&gt; result)
/// {
///     if (result.IsSuccess)
///         return Ok(result.Value);
///     
///     var statusCode = result.Failure.ErrorCode switch
///     {
///         ErrorCode.BadRequest => 400,
///         ErrorCode.Unauthorized => 401,
///         ErrorCode.NotFound => 404,
///         ErrorCode.UnprocessableEntity => 422,
///         ErrorCode.Error => 500,
///         _ => 500
///     };
///     
///     return StatusCode(statusCode, new { error = result.Failure.Description });
/// }
/// </code>
/// </example>
public enum ErrorCode
{
    /// <summary>
    /// Generic error code for unexpected system errors, exceptions, and internal failures.
    /// Maps to HTTP 500 Internal Server Error.
    /// </summary>
    Error,

    /// <summary>
    /// Error code for cases where a requested resource or entity cannot be found.
    /// Maps to HTTP 404 Not Found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Error code for authentication and authorization failures.
    /// Maps to HTTP 401 Unauthorized.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Error code for business rule violations and semantic errors.
    /// Maps to HTTP 422 Unprocessable Entity.
    /// </summary>
    UnprocessableEntity,

    /// <summary>
    /// Error code for invalid client input, malformed requests, and data validation failures.
    /// Maps to HTTP 400 Bad Request.
    /// </summary>
    BadRequest
}
