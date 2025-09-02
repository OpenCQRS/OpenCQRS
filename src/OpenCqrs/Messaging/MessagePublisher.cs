using OpenCqrs.Results;

namespace OpenCqrs.Messaging;

public class MessagePublisher(IMessagingProvider messagingProvider) : IMessagePublisher
{
    public async Task<Result> Publish<TMessage>(TMessage message) where TMessage : IMessage
    {
        if (message is IQueueMessage && message is ITopicMessage)
        {
            throw new Exception("The message cannot implement both the IQueueMessage and the ITopicMessage interfaces");
        }

        if (message is IQueueMessage queueMessage)
        {
            return await messagingProvider.SendQueueMessage(queueMessage);
        }

        if (message is ITopicMessage topicMessage)
        {
            return await messagingProvider.SendTopicMessage(topicMessage);
        }

        throw new NotSupportedException("The message must implement either the IQueueMessage or the ITopicMessage interface");
    }
}
