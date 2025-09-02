namespace OpenCqrs.Messaging;

public abstract class QueueMessage : Message, IQueueMessage
{
    public required string QueueName { get; set; }
}
