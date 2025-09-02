namespace OpenCqrs.Messaging;

public interface IMessagePublisher
{
    Task Publish<TMessage>(TMessage message) where TMessage : IMessage;
}
