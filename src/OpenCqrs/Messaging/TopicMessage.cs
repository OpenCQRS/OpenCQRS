namespace OpenCqrs.Messaging;

public abstract class TopicMessage : Message, ITopicMessage
{
    public required string TopicName { get; set; }
}
