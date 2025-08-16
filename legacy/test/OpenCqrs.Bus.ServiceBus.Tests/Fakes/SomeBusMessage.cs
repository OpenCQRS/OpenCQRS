using System;
using System.Collections.Generic;

namespace OpenCqrs.Bus.ServiceBus.Tests.Fakes
{
    internal class SomeBusMessage : IBusMessage
    {
        public DateTime? ScheduledEnqueueTimeUtc { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }
}
