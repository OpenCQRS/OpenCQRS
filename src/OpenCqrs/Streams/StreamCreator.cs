using System.Collections.Concurrent;

namespace OpenCqrs.Streams;

public class StreamCreator(IServiceProvider serviceProvider) : IStreamCreator
{
    private static readonly ConcurrentDictionary<Type, object?> StreamHandlerWrappers = new();

    public IAsyncEnumerable<TResponse> Create<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var streamRequestType = request.GetType();
        var handler = (StreamRequestHandlerWrapperBase<TResponse>)StreamHandlerWrappers.GetOrAdd(streamRequestType, _ => 
            Activator.CreateInstance(typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(streamRequestType, typeof(TResponse))))!;

        return handler.Handle(request, serviceProvider, cancellationToken);
    }
}
