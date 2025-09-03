using OpenCqrs.Messaging;

namespace OpenCqrs.Messaging.RabbitMq.Tests.Models;

public class TestTopicMessage : ITopicMessage
{
    public string TopicName { get; set; } = string.Empty;
    public DateTime? ScheduledEnqueueTimeUtc { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public string TestData { get; set; } = string.Empty;
}
