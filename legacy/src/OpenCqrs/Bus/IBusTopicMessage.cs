namespace OpenCqrs.Bus
{
    public interface IBusTopicMessage : IBusMessage
    {
        string TopicName { get; set; }
    }
}
