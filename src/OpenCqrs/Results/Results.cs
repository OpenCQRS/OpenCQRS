using OneOf;

namespace OpenCqrs.Results;

public sealed class Result : OneOfBase<Success, Failure>
{
    private Result(OneOf<Success, Failure> input) : base(input) { }

    public static implicit operator Result(Success success) => new(success);
    public static implicit operator Result(Failure failure) => new(failure);

    public bool IsSuccess => IsT0;
    public bool IsFailure => IsT1;
    
    public bool IsNotFailure => IsT0;
    public bool IsNotSuccess => IsT1;

    public Success? Success => IsT0 ? AsT0 : null;
    public Failure? Failure => IsT1 ? AsT1 : null;

    public static Result Ok() => new(new Success());
    public static Result Ok(Success success) => new(success);
    public static Result Fail(ErrorCode errorCode = ErrorCode.Error, string? title = null, string? description = null, string? type = null, IDictionary<string, string>? tags = null) => new(new Failure(errorCode, title, description, type, tags));
    public static Result Fail(Failure failure) => new(failure);
    
    public bool TryPickSuccess(out Success success, out Failure failure) => TryPickT0(out success, out failure);
    public bool TryPickFailure(out Failure failure, out Success success) => TryPickT1(out failure, out success);
}

public sealed class Result<TValue> : OneOfBase<Success<TValue>, Failure>
{
    private Result(OneOf<Success<TValue>, Failure> input) : base(input) { }

    public static implicit operator Result<TValue>(Success<TValue> success) => new(success);
    public static implicit operator Result<TValue>(Failure failure) => new(failure);
    public static implicit operator Result<TValue>(TValue result) => new(new Success<TValue>(result));

    public bool IsSuccess => IsT0;
    public bool IsFailure => IsT1;
    
    public bool IsNotFailure => IsT0;
    public bool IsNotSuccess => IsT1;

    public Success<TValue>? Success => IsT0 ? AsT0 : null;
    public Failure? Failure => IsT1 ? AsT1 : null;
    public new TValue? Value => IsT0 ? AsT0.Result : default;

    public static Result<TValue> Ok(TValue result) => new(new Success<TValue>(result));
    public static Result<TValue> Ok(Success<TValue> success) => new(success);
    public static Result<TValue> Fail(ErrorCode errorCode = ErrorCode.Error, string? title = null, string? description = null, string? type = null, IDictionary<string, string>? tags = null) => new(new Failure(errorCode, title, description, type, tags));
    public static Result<TValue> Fail(Failure failure) => new(failure);

    public bool TryPickSuccess(out Success<TValue> success, out Failure failure) => TryPickT0(out success, out failure);
    public bool TryPickFailure(out Failure failure, out Success<TValue> success) => TryPickT1(out failure, out success);
}
