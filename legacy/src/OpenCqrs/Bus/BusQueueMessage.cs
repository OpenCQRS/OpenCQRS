namespace OpenCqrs.Bus
{
    public abstract class BusQueueMessage : BusMessage, IBusQueueMessage
    {
        public string QueueName { get; set; }
    }
}
