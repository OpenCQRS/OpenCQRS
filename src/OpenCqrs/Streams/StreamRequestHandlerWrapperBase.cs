using Microsoft.Extensions.DependencyInjection;

namespace OpenCqrs.Streams;

internal abstract class StreamRequestHandlerWrapperBase<TResponse>
{
    protected static THandler? GetHandler<THandler>(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<THandler>();
    }

    public abstract IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> streamRequest, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
