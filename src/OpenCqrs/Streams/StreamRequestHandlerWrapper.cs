using System.Runtime.CompilerServices;

namespace OpenCqrs.Streams;

internal class StreamRequestHandlerWrapper<TStreamRequest, TResponse> : StreamRequestHandlerWrapperBase<TResponse> where TStreamRequest : IStreamRequest<TResponse>
{
    public override async IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> streamRequest, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var handler = GetHandler<IStreamRequestHandler<TStreamRequest, TResponse>>(serviceProvider);

        if (handler == null)
        {
            throw new Exception("Stream request handler not found.");
        }

        await foreach (var item in handler.Handle((TStreamRequest)streamRequest, cancellationToken))
        {
            yield return item;
        }
    }
}