namespace OpenCqrs.Messaging;

public interface ITopicMessage : IMessage
{
    string TopicName { get; set; }
}
