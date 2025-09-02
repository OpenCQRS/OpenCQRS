using OpenCqrs.Results;

namespace OpenCqrs.Messaging;

public interface IMessagePublisher
{
    Task<Result> Publish<TMessage>(TMessage message) where TMessage : IMessage;
}
