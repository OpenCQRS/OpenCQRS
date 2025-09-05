namespace OpenCqrs.Messaging.ServiceBus.Tests.Models;

public class TestQueueMessage : IQueueMessage
{
    public string QueueName { get; set; } = string.Empty;
    public DateTime? ScheduledEnqueueTimeUtc { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public string TestData { get; set; } = string.Empty;
}
