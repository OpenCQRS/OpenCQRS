namespace OpenCqrs.Messaging;

public interface IMessage
{
    DateTime? ScheduledEnqueueTimeUtc { get; set; }
    IDictionary<string, object> Properties { get; set; }
}
