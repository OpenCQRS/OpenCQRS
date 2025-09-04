using OpenCqrs.Results;

namespace OpenCqrs.Messaging;

public class DefaultMessagingProvider : IMessagingProvider
{
    public Task<Result> SendQueueMessage<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IQueueMessage
    {
        throw new NotImplementedException();
    }

    public Task<Result> SendTopicMessage<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : ITopicMessage
    {
        throw new NotImplementedException();
    }
}
