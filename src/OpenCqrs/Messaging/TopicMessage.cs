using Newtonsoft.Json;

namespace OpenCqrs.Messaging;

public abstract class TopicMessage : Message, ITopicMessage
{
    [JsonIgnore]
    public required string TopicName { get; set; }
}
