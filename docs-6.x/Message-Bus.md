# Message Bus

OpenCQRS supports Azure Service Bus and RabbitMQ and commands/events can be automatically sent to a bus.

**Azure Service Bus**

First you need to create your own namespace and queue(s)/topic(s) on Azure.
Queues and Topics are **NOT** currently created automatically by OpenCQRS, so you need to create them manually using the Azure Portal.

Please, follow the instruction contained on the following page: https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues.

Once your connection string and queue(s) are created, you need to update the configuration in the appsettings.json:

```JSON
{
  "ConnectionStrings": {
    "OpenCqrsMessageBus": "your-azure-service-bus-connection-string",
  }
}
```

Using this functionality is really simple.
Your event class just needs to implement either the IBusQueueMessage interface or the IBusTopicMessage interface:

```C#
public class SomethingHappened : DomainEvent, IBusQueueMessage
{
        public string QueueName { get; set; } = "something-happened";
        public IDictionary<string, object> Properties { get; set; }
}
```

Where QueueName is obviously the name of the queue you want your message to be sent to. Properties is a dictionary used to set all additional settings of the bus message (e.g. ScheduledEnqueueTimeUtc).

A message can also be sent directly using the IDispatcher interface:

```C#
var command = new DoSomething(); // It needs to implement either IBusQueueMessage or IBusTopicMessage
await _dispatcher.DispatchBusMessageAsync(command)
```
