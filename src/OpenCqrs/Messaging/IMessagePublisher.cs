using OpenCqrs.Results;

namespace OpenCqrs.Messaging;

public interface IMessagePublisher
{
    Task<Result> Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;
}
