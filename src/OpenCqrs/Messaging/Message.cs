using Newtonsoft.Json;

namespace OpenCqrs.Messaging;

public abstract class Message : IMessage
{
    [JsonIgnore]
    public DateTime? ScheduledEnqueueTimeUtc { get; set; }
    
    [JsonIgnore]
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
