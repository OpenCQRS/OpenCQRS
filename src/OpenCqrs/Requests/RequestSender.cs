using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Results;

namespace OpenCqrs.Requests;

public class RequestSender(IServiceProvider serviceProvider) : IRequestSender
{
    private static readonly ConcurrentDictionary<Type, object?> RequestHandlerWrappers = new();

    public async Task<Result> Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();

        if (handler is null)
        {
            throw new Exception("Request handler not found.");
        }

        return await handler.Handle(request, cancellationToken);
    }

    public async Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var handler = (RequestHandlerWrapperBase<TResponse>)RequestHandlerWrappers.GetOrAdd(requestType, _ => 
            Activator.CreateInstance(typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))))!;

        var result = await handler.Handle(request, serviceProvider, cancellationToken);

        return result;
    }
}
