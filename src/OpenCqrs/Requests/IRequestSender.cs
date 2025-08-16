using OpenCqrs.Results;

namespace OpenCqrs.Requests;

public interface IRequestSender
{
    Task<Result> Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
    Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
