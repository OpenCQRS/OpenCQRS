using System;
using System.Collections.Generic;
using OpenCqrs.Bus;
using OpenCqrs.Events;

namespace OpenCqrs.Tests.Fakes
{
    public class SomethingCreated : Event, IBusQueueMessage
    {
        public DateTime? ScheduledEnqueueTimeUtc { get; set; }
        public string QueueName { get; set; } = "queue-name";
        public IDictionary<string, object> Properties { get; set; }
    }
}
