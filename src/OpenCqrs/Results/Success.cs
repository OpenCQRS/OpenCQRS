namespace OpenCqrs.Results;

/// <summary>
/// Represents a successful operation result without any associated data.
/// Used in the Result pattern to indicate that an operation completed successfully.
/// </summary>
/// <example>
/// <code>
/// // Creating success instances
/// var success1 = new Success();
/// var success2 = new Success();
/// 
/// // Success instances are equal by value
/// Assert.True(success1 == success2);
/// 
/// // Using in Result pattern
/// public Result DeleteUser(Guid userId)
/// {
///     if (!UserExists(userId))
///         return Result.Fail(ErrorCode.NotFound, "User not found");
///     
///     DeleteUserFromDatabase(userId);
///     return new Success(); // or Result.Ok()
/// }
/// 
/// // Implicit conversion to Result
/// Result result = new Success();
/// Assert.True(result.IsSuccess);
/// </code>
/// </example>
public record Success;

/// <summary>
/// Represents a successful operation result that contains associated data of type <typeparamref name="TResult"/>.
/// Used in the Result pattern to indicate successful completion while providing access to the operation's output.
/// </summary>
/// <typeparam name="TResult">
/// The type of data contained in the successful result. Can be any type including primitives,
/// complex objects, collections, or value types.
/// </typeparam>
/// <example>
/// <code>
/// // Creating success instances with data
/// var userSuccess = new Success&lt;User&gt;(new User("john@example.com"));
/// var listSuccess = new Success&lt;List&lt;string&gt;&gt;(new List&lt;string&gt; { "item1", "item2" });
/// 
/// // Default constructor for nullable results
/// var emptySuccess = new Success&lt;User?&gt;(); // Result will be null
/// 
/// // Using in query operations
/// public Result&lt;UserDto&gt; GetUserById(Guid userId)
/// {
///     var user = _repository.GetById(userId);
///     if (user == null)
///         return Result&lt;UserDto&gt;.Fail(ErrorCode.NotFound, "User not found");
///     
///     var userDto = _mapper.Map&lt;UserDto&gt;(user);
///     return new Success&lt;UserDto&gt;(userDto);
/// }
/// 
/// // Accessing result data
/// var result = GetUserById(userId);
/// if (result.IsSuccess)
/// {
///     var userData = result.Success?.Result; // Access the contained data
///     Console.WriteLine($"User: {userData?.Name}");
/// }
/// 
/// // Value equality with records
/// var success1 = new Success&lt;int&gt;(42);
/// var success2 = new Success&lt;int&gt;(42);
/// Assert.True(success1 == success2); // True - same value
/// 
/// var success3 = new Success&lt;int&gt;(99);
/// Assert.False(success1 == success3); // False - different values
/// </code>
/// </example>
public record Success<TResult>
{
    /// <summary>
    /// Gets the result data from the successful operation.
    /// </summary>
    /// <value>
    /// The data produced by the successful operation, or <c>null</c> if no data was provided
    /// or if <typeparamref name="TResult"/> is a nullable type.
    /// </value>
    public TResult? Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Success{TResult}"/> record with no result data.
    /// </summary>
    public Success()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Success{TResult}"/> record with the specified result data.
    /// </summary>
    /// <param name="result">The result data to associate with this successful operation.</param>
    public Success(TResult result)
    {
        Result = result;
    }
}
