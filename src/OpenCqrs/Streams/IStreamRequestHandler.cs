namespace OpenCqrs.Streams;

public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
