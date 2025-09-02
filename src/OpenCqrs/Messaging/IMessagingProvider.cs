namespace OpenCqrs.Messaging;

public interface IMessagingProvider
{
    Task SendQueueMessage<TMessage>(TMessage message) where TMessage : IQueueMessage;
    
    Task SendTopicMessage<TMessage>(TMessage message) where TMessage : ITopicMessage;
}
