using System.Collections.Concurrent;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using OpenCqrs.Results;

namespace OpenCqrs.Messaging.ServiceBus;

public class ServiceBusMessagingProvider(ServiceBusClient serviceBusClient) : IMessagingProvider, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _queueSenders = new();
    private readonly ConcurrentDictionary<string, ServiceBusSender> _topicSenders = new();

    public async Task<Result> SendQueueMessage<TMessage>(TMessage message) where TMessage : IQueueMessage
    {
        try
        {
            if (string.IsNullOrEmpty(message.QueueName))
            {
                return new Failure(Title: "Queue name", Description: "Queue name cannot be null or empty");
            }

            var sender = GetQueueSender(message.QueueName);
            var serviceBusMessage = CreateServiceBusMessage(message);

            await sender.SendMessageAsync(serviceBusMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var tagList = new TagList { { "Operation description", "Sending queue message" } };
            Activity.Current?.AddException(ex, tagList, DateTimeOffset.UtcNow);
            return new Failure
            (
                Title: "Error",
                Description: "There was an error when processing the request"
            );
        }
    }

    public async Task<Result> SendTopicMessage<TMessage>(TMessage message) where TMessage : ITopicMessage
    {
        try
        {
            if (string.IsNullOrEmpty(message.TopicName))
            {
                return new Failure(Title: "Topic name", Description: "Topic name cannot be null or empty");
            }

            var sender = GetTopicSender(message.TopicName);
            var serviceBusMessage = CreateServiceBusMessage(message);

            await sender.SendMessageAsync(serviceBusMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var tagList = new TagList { { "Operation description", "Sending topic message" } };
            Activity.Current?.AddException(ex, tagList, DateTimeOffset.UtcNow);
            return new Failure
            (
                Title: "Error",
                Description: "There was an error when processing the request"
            );
        }
    }

    private ServiceBusSender GetQueueSender(string queueName)
    {
        if (_queueSenders.TryGetValue(queueName, out var queueSender))
        {
            return queueSender;
        }

        queueSender = serviceBusClient.CreateSender(queueName);
        _queueSenders[queueName] = queueSender;

        return queueSender;
    }

    private ServiceBusSender GetTopicSender(string topicName)
    {
        if (_topicSenders.TryGetValue(topicName, out var topicSender))
        {
            return topicSender;
        }

        topicSender = serviceBusClient.CreateSender(topicName);
        _topicSenders[topicName] = topicSender;

        return topicSender;
    }

    private ServiceBusMessage CreateServiceBusMessage<TMessage>(TMessage message) where TMessage : IMessage
    {
        var json = JsonConvert.SerializeObject(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        if (message.ScheduledEnqueueTimeUtc.HasValue)
        {
            serviceBusMessage.ScheduledEnqueueTime = message.ScheduledEnqueueTimeUtc.Value;
        }

        foreach (var property in message.Properties)
        {
            serviceBusMessage.ApplicationProperties[property.Key] = property.Value;
        }

        return serviceBusMessage;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _queueSenders.Values)
        {
            await sender.DisposeAsync();
        }

        foreach (var sender in _topicSenders.Values)
        {
            await sender.DisposeAsync();
        }

        await serviceBusClient.DisposeAsync();

        _queueSenders.Clear();
        _topicSenders.Clear();
    }
}
