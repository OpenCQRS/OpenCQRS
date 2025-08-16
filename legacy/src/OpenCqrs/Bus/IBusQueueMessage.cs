namespace OpenCqrs.Bus
{
    public interface IBusQueueMessage : IBusMessage
    {
        string QueueName { get; set; }
    }
}
