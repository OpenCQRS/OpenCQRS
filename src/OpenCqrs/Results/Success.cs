namespace OpenCqrs.Results;

public record Success;

public record Success<TResult>
{
    public TResult? Result { get; }

    public Success()
    {
    }
    
    public Success(TResult result)
    {
        Result = result;
    }
}
