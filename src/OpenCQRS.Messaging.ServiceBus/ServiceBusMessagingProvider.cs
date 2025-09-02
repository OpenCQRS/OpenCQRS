using OpenCqrs.Messaging;
using OpenCqrs.Results;

namespace OpenCQRS.Messaging.ServiceBus;

public class ServiceBusMessagingProvider : IMessagingProvider
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
