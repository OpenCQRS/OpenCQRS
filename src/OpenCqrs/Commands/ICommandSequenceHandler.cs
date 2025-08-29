using OpenCqrs.Results;

namespace OpenCqrs.Commands;

public interface ICommandSequenceHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, IEnumerable<Result<TResponse>> previousCommandResults, CancellationToken cancellationToken = default);
}
