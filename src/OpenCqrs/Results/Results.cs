using OneOf;

namespace OpenCqrs.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail, without returning a value.
/// Implements the Result pattern to provide type-safe error handling and eliminate the need for exceptions
/// in business logic flows.
/// </summary>
/// <example>
/// <code>
/// // Creating success results
/// var success = Result.Ok();
/// var successWithCustom = Result.Ok(new Success());
/// 
/// // Creating failure results
/// var failure = Result.Fail(ErrorCode.BadRequest, "Invalid input", "The provided data is invalid");
/// var customFailure = Result.Fail(new Failure(ErrorCode.NotFound, "User not found"));
/// 
/// // Using results in business logic
/// public async Task&lt;Result&gt; CreateUserAsync(CreateUserCommand command)
/// {
///     if (string.IsNullOrEmpty(command.Email))
///         return Result.Fail(ErrorCode.BadRequest, "Invalid Email", "Email address is required");
///     
///     var existingUser = await _userRepository.GetByEmailAsync(command.Email);
///     if (existingUser != null)
///         return Result.Fail(ErrorCode.UnprocessableEntity, "User Exists", "A user with this email already exists");
///     
///     await _userRepository.AddAsync(new User(command.Email));
///     return Result.Ok();
/// }
/// 
/// // Consuming results
/// var result = await CreateUserAsync(command);
/// if (result.IsSuccess)
/// {
///     logger.LogInformation("User created successfully");
/// }
/// else
/// {
///     logger.LogError("Failed to create user: {Error}", result.Failure?.Description);
/// }
/// 
/// // Pattern matching with results
/// var message = result.Match(
///     success => "Operation completed successfully",
///     failure => $"Operation failed: {failure.Description}"
/// );
/// </code>
/// </example>
public sealed class Result : OneOfBase<Success, Failure>
{
    /// <summary>
    /// Initializes a new instance of the Result class with the specified OneOf union.
    /// </summary>
    /// <param name="input">The OneOf union containing either a Success or Failure state.</param>
    private Result(OneOf<Success, Failure> input) : base(input) { }

    /// <summary>
    /// Implicitly converts a Success instance to a Result.
    /// </summary>
    /// <param name="success">The success instance to convert.</param>
    /// <returns>A Result representing the successful operation.</returns>
    public static implicit operator Result(Success success) => new(success);

    /// <summary>
    /// Implicitly converts a Failure instance to a Result.
    /// </summary>
    /// <param name="failure">The failure instance to convert.</param>
    /// <returns>A Result representing the failed operation.</returns>
    public static implicit operator Result(Failure failure) => new(failure);

    /// <summary>
    /// Gets a value indicating whether the result represents a successful operation.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    public bool IsSuccess => IsT0;

    /// <summary>
    /// Gets a value indicating whether the result represents a failed operation.
    /// </summary>
    /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
    public bool IsFailure => IsT1;

    /// <summary>
    /// Gets a value indicating whether the result does not represent a failed operation.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    public bool IsNotFailure => IsT0;

    /// <summary>
    /// Gets a value indicating whether the result does not represent a successful operation.
    /// </summary>
    /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
    public bool IsNotSuccess => IsT1;

    /// <summary>
    /// Gets the Success instance if the result represents a successful operation.
    /// </summary>
    /// <value>The Success instance if successful; otherwise, <c>null</c>.</value>
    public Success? Success => IsT0 ? AsT0 : null;

    /// <summary>
    /// Gets the Failure instance if the result represents a failed operation.
    /// </summary>
    /// <value>The Failure instance if failed; otherwise, <c>null</c>.</value>
    public Failure? Failure => IsT1 ? AsT1 : null;

    /// <summary>
    /// Creates a successful result with a default Success instance.
    /// </summary>
    /// <returns>A Result representing a successful operation.</returns>
    public static Result Ok() => new(new Success());

    /// <summary>
    /// Creates a successful result with the specified Success instance.
    /// </summary>
    /// <param name="success">The Success instance to use for the result.</param>
    /// <returns>A Result representing a successful operation.</returns>
    public static Result Ok(Success success) => new(success);

    /// <summary>
    /// Creates a failed result with the specified error information.
    /// </summary>
    /// <param name="errorCode">The error code indicating the type of failure.</param>
    /// <param name="title">Optional title for the error.</param>
    /// <param name="description">Optional detailed description of the error.</param>
    /// <param name="type">Optional error type categorization.</param>
    /// <param name="tags">Optional dictionary of additional error metadata.</param>
    /// <returns>A Result representing a failed operation.</returns>
    public static Result Fail(ErrorCode errorCode = ErrorCode.Error, string? title = null, string? description = null, string? type = null, IDictionary<string, string>? tags = null) => new(new Failure(errorCode, title, description, type, tags));

    /// <summary>
    /// Creates a failed result with the specified Failure instance.
    /// </summary>
    /// <param name="failure">The Failure instance to use for the result.</param>
    /// <returns>A Result representing a failed operation.</returns>
    public static Result Fail(Failure failure) => new(failure);

    /// <summary>
    /// Attempts to extract the Success and Failure instances from the result.
    /// </summary>
    /// <param name="success">When this method returns, contains the Success instance if the result is successful.</param>
    /// <param name="failure">When this method returns, contains the Failure instance if the result is failed.</param>
    /// <returns><c>true</c> if the result is successful; otherwise, <c>false</c>.</returns>
    public bool TryPickSuccess(out Success success, out Failure failure) => TryPickT0(out success, out failure);

    /// <summary>
    /// Attempts to extract the Failure and Success instances from the result.
    /// </summary>
    /// <param name="failure">When this method returns, contains the Failure instance if the result is failed.</param>
    /// <param name="success">When this method returns, contains the Success instance if the result is successful.</param>
    /// <returns><c>true</c> if the result is failed; otherwise, <c>false</c>.</returns>
    public bool TryPickFailure(out Failure failure, out Success success) => TryPickT1(out failure, out success);
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// Implements the Result pattern to provide type-safe error handling with strongly-typed return values.
/// </summary>
/// <typeparam name="TValue">The type of value returned by successful operations.</typeparam>
/// <example>
/// <code>
/// // Creating success results with values
/// var successResult = Result&lt;User&gt;.Ok(new User("john@example.com"));
/// var implicitSuccess = (User)new User("jane@example.com"); // Implicit conversion
/// 
/// // Creating failure results
/// var notFound = Result&lt;User&gt;.Fail(ErrorCode.NotFound, "User Not Found", "No user found with the specified ID");
/// 
/// // Using in query handlers
/// public async Task&lt;Result&lt;UserDto&gt;&gt; GetUserByIdAsync(Guid userId)
/// {
///     var user = await _userRepository.GetByIdAsync(userId);
///     if (user == null)
///         return Result&lt;UserDto&gt;.Fail(ErrorCode.NotFound, "User not found");
///     
///     var userDto = _mapper.Map&lt;UserDto&gt;(user);
///     return Result&lt;UserDto&gt;.Ok(userDto);
/// }
/// 
/// // Consuming results with values
/// var result = await GetUserByIdAsync(userId);
/// if (result.IsSuccess)
/// {
///     var user = result.Value;
///     Console.WriteLine($"Found user: {user.Email}");
/// }
/// else
/// {
///     logger.LogWarning("Failed to get user: {Error}", result.Failure?.Description);
/// }
/// 
/// // Pattern matching with typed results
/// var response = result.Match(
///     success => new ApiResponse&lt;UserDto&gt; { Data = success.Result, IsSuccess = true },
///     failure => new ApiResponse&lt;UserDto&gt; { Error = failure.Description, IsSuccess = false }
/// );
/// 
/// // Chaining operations
/// var finalResult = await GetUserByIdAsync(userId)
///     .Match(
///         success => ProcessUserAsync(success.Result),
///         failure => Task.FromResult(Result&lt;ProcessedUser&gt;.Fail(failure))
///     );
/// </code>
/// </example>
public sealed class Result<TValue> : OneOfBase<Success<TValue>, Failure>
{
    /// <summary>
    /// Initializes a new instance of the Result class with the specified OneOf union.
    /// </summary>
    /// <param name="input">The OneOf union containing either a Success or Failure state.</param>
    private Result(OneOf<Success<TValue>, Failure> input) : base(input) { }

    /// <summary>
    /// Implicitly converts a Success instance to a Result.
    /// </summary>
    /// <param name="success">The success instance to convert.</param>
    /// <returns>A Result representing the successful operation with a value.</returns>
    public static implicit operator Result<TValue>(Success<TValue> success) => new(success);

    /// <summary>
    /// Implicitly converts a Failure instance to a Result.
    /// </summary>
    /// <param name="failure">The failure instance to convert.</param>
    /// <returns>A Result representing the failed operation.</returns>
    public static implicit operator Result<TValue>(Failure failure) => new(failure);

    /// <summary>
    /// Implicitly converts a value to a successful Result.
    /// </summary>
    /// <param name="result">The value to wrap in a successful result.</param>
    /// <returns>A Result representing a successful operation with the specified value.</returns>
    public static implicit operator Result<TValue>(TValue result) => new(new Success<TValue>(result));

    /// <summary>
    /// Gets a value indicating whether the result represents a successful operation.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    public bool IsSuccess => IsT0;

    /// <summary>
    /// Gets a value indicating whether the result represents a failed operation.
    /// </summary>
    /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
    public bool IsFailure => IsT1;

    /// <summary>
    /// Gets a value indicating whether the result does not represent a failed operation.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    public bool IsNotFailure => IsT0;

    /// <summary>
    /// Gets a value indicating whether the result does not represent a successful operation.
    /// </summary>
    /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
    public bool IsNotSuccess => IsT1;

    /// <summary>
    /// Gets the Success instance if the result represents a successful operation.
    /// </summary>
    /// <value>The Success instance if successful; otherwise, <c>null</c>.</value>
    public Success<TValue>? Success => IsT0 ? AsT0 : null;

    /// <summary>
    /// Gets the Failure instance if the result represents a failed operation.
    /// </summary>
    /// <value>The Failure instance if failed; otherwise, <c>null</c>.</value>
    public Failure? Failure => IsT1 ? AsT1 : null;

    /// <summary>
    /// Gets the value from a successful result.
    /// </summary>
    /// <value>The result value if successful; otherwise, the default value for <typeparamref name="TValue"/>.</value>
    public new TValue? Value => IsT0 ? AsT0.Result : default;

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="result">The value to include in the successful result.</param>
    /// <returns>A Result representing a successful operation with the specified value.</returns>
    public static Result<TValue> Ok(TValue result) => new(new Success<TValue>(result));

    /// <summary>
    /// Creates a successful result with the specified Success instance.
    /// </summary>
    /// <param name="success">The Success instance to use for the result.</param>
    /// <returns>A Result representing a successful operation.</returns>
    public static Result<TValue> Ok(Success<TValue> success) => new(success);

    /// <summary>
    /// Creates a failed result with the specified error information.
    /// </summary>
    /// <param name="errorCode">The error code indicating the type of failure.</param>
    /// <param name="title">Optional title for the error.</param>
    /// <param name="description">Optional detailed description of the error.</param>
    /// <param name="type">Optional error type categorization.</param>
    /// <param name="tags">Optional dictionary of additional error metadata.</param>
    /// <returns>A Result representing a failed operation.</returns>
    public static Result<TValue> Fail(ErrorCode errorCode = ErrorCode.Error, string? title = null, string? description = null, string? type = null, IDictionary<string, string>? tags = null) => new(new Failure(errorCode, title, description, type, tags));

    /// <summary>
    /// Creates a failed result with the specified Failure instance.
    /// </summary>
    /// <param name="failure">The Failure instance to use for the result.</param>
    /// <returns>A Result representing a failed operation.</returns>
    public static Result<TValue> Fail(Failure failure) => new(failure);

    /// <summary>
    /// Attempts to extract the Success and Failure instances from the result.
    /// </summary>
    /// <param name="success">When this method returns, contains the Success instance if the result is successful.</param>
    /// <param name="failure">When this method returns, contains the Failure instance if the result is failed.</param>
    /// <returns><c>true</c> if the result is successful; otherwise, <c>false</c>.</returns>
    public bool TryPickSuccess(out Success<TValue> success, out Failure failure) => TryPickT0(out success, out failure);

    /// <summary>
    /// Attempts to extract the Failure and Success instances from the result.
    /// </summary>
    /// <param name="failure">When this method returns, contains the Failure instance if the result is failed.</param>
    /// <param name="success">When this method returns, contains the Success instance if the result is successful.</param>
    /// <returns><c>true</c> if the result is failed; otherwise, <c>false</c>.</returns>
    public bool TryPickFailure(out Failure failure, out Success<TValue> success) => TryPickT1(out failure, out success);
}
