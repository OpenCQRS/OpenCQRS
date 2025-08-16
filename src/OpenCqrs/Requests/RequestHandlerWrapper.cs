using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Results;

namespace OpenCqrs.Requests;

internal class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse> where TRequest : IRequest<TResponse>
{
    public override async Task<Result<TResponse>> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = GetHandler<IRequestHandler<TRequest, TResponse>>(serviceProvider);

        if (handler == null)
        {
            throw new Exception("Request handler not found.");
        }

        return await handler.Handle((TRequest) request, cancellationToken);
    }
}

internal abstract class RequestHandlerWrapperBase<TResult>
{
    protected static THandler? GetHandler<THandler>(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<THandler>();
    }

    public abstract Task<Result<TResult>> Handle(IRequest<TResult> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
