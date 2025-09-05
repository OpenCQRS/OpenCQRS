using Newtonsoft.Json;

namespace OpenCqrs.Messaging;

public abstract class QueueMessage : Message, IQueueMessage
{
    [JsonIgnore]
    public required string QueueName { get; set; }
}
