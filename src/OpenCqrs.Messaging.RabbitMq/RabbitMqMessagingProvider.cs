using OpenCqrs.Messaging;
using OpenCqrs.Results;

namespace OpenCqrs.Messaging.RabbitMq;

public class RabbitMqMessagingProvider : IMessagingProvider
{
    public Task<Result> SendQueueMessage<TMessage>(TMessage message) where TMessage : IQueueMessage
    {
        throw new NotImplementedException();
    }

    public Task<Result> SendTopicMessage<TMessage>(TMessage message) where TMessage : ITopicMessage
    {
        throw new NotImplementedException();
    }
}
