using OpenCqrs.Results;

namespace OpenCqrs.Messaging;

public interface IMessagingProvider
{
    Task<Result> SendQueueMessage<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IQueueMessage;

    Task<Result> SendTopicMessage<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : ITopicMessage;
}
