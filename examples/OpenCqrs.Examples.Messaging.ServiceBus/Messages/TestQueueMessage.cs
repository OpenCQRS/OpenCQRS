using OpenCqrs.Messaging;

namespace OpenCqrs.Examples.Messaging.ServiceBus.Messages;

public class TestQueueMessage : QueueMessage
{
    public string TestData { get; set; } = string.Empty;
}
