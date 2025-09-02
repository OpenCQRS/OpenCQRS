namespace OpenCqrs.Messaging;

public abstract class Message : IMessage
{
    public DateTime? ScheduledEnqueueTimeUtc { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
