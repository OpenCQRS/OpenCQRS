using System;
using System.Collections.Generic;
using OpenCqrs.Bus;
using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Fakes
{
    public class CreateAggregateBusMessage : DomainCommand<Aggregate>, IBusQueueMessage
    {
        public DateTime? ScheduledEnqueueTimeUtc { get; set; }
        public string QueueName { get; set; } = "create-something";
        public IDictionary<string, object> Properties { get; set; }
    }
}
