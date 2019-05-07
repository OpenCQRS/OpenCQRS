﻿using System;
using System.Collections.Generic;
using OpenCqrs.Bus;
using OpenCqrs.Domain;

namespace OpenCqrs.Examples.Domain.Events
{
    public class ProductCreatedBusMessage : DomainEvent, IBusTopicMessage
    {
        public string Title { get; set; }
        public ProductStatus Status { get; set; }

        public DateTime? ScheduledEnqueueTimeUtc { get; set; }
        public string TopicName { get; set; } = "product-created";
        public IDictionary<string, object> Properties { get; set; }
    }
}
