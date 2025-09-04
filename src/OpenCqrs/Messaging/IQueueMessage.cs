namespace OpenCqrs.Messaging;

public interface IQueueMessage : IMessage
{
    string QueueName { get; set; }
}
