using OpenCqrs.Notifications;
using OpenCqrs.Requests;
using OpenCqrs.Results;

namespace OpenCqrs;

public interface IDispatcher
{
    Task<Result> Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
    Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task<IEnumerable<Result>> Publish<TNotification>(INotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}
