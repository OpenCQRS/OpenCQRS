using Azure.Messaging.ServiceBus;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Collections.Concurrent;
using System.Text.Json;
using OpenCqrs.Messaging.ServiceBus.Tests.Messages;

namespace OpenCqrs.Messaging.ServiceBus.Tests;

public static class MockServiceBusTestHelper
{
    public static ServiceBusClient MockServiceBusClient { get; }

    private static readonly ConcurrentDictionary<string, ServiceBusSender> MockQueueSenders = new();
    private static readonly ConcurrentDictionary<string, ServiceBusSender> MockTopicSenders = new();
    private static readonly ConcurrentBag<SentMessage> SentMessages = [];
    private static readonly ConcurrentDictionary<string, Exception> SendFailures = new();

    static MockServiceBusTestHelper()
    {
        MockServiceBusClient = Substitute.For<ServiceBusClient>();
        SetupDefaultBehavior();
    }

    private static void SetupDefaultBehavior()
    {
        MockServiceBusClient.CreateSender(Arg.Any<string>())
            .Returns(call =>
            {
                var entityName = call.Arg<string>();
                var sender = CreateMockSender(entityName);

                MockQueueSenders.TryAdd(entityName, sender);
                MockTopicSenders.TryAdd(entityName, sender);

                return sender;
            });
    }

    private static ServiceBusSender CreateMockSender(string entityName)
    {
        var sender = Substitute.For<ServiceBusSender>();

        sender.SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (SendFailures.TryGetValue(entityName, out var exception))
                {
                    throw exception;
                }

                var message = call.Arg<ServiceBusMessage>();

                SentMessages.Add(new SentMessage
                {
                    EntityName = entityName,
                    ServiceBusMessage = message,
                    SentAt = DateTimeOffset.UtcNow,
                    MessageBody = message.Body.ToString(),
                    ContentType = message.ContentType,
                    MessageId = message.MessageId,
                    ScheduledEnqueueTime = message.ScheduledEnqueueTime,
                    ApplicationProperties = new Dictionary<string, object>(message.ApplicationProperties),
                    OriginalMessageType = GetOriginalMessageType(message.Body.ToString())
                });

                return Task.CompletedTask;
            });

        sender.SendMessagesAsync(Arg.Any<IEnumerable<ServiceBusMessage>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (SendFailures.TryGetValue(entityName, out var exception))
                {
                    throw exception;
                }

                var messages = call.Arg<IEnumerable<ServiceBusMessage>>();

                foreach (var message in messages)
                {
                    SentMessages.Add(new SentMessage
                    {
                        EntityName = entityName,
                        ServiceBusMessage = message,
                        SentAt = DateTimeOffset.UtcNow,
                        MessageBody = message.Body.ToString(),
                        ContentType = message.ContentType,
                        MessageId = message.MessageId,
                        ScheduledEnqueueTime = message.ScheduledEnqueueTime,
                        ApplicationProperties = new Dictionary<string, object>(message.ApplicationProperties),
                        OriginalMessageType = GetOriginalMessageType(message.Body.ToString())
                    });
                }

                return Task.CompletedTask;
            });

        sender.DisposeAsync().Returns(ValueTask.CompletedTask);

        return sender;
    }

    private static string? GetOriginalMessageType(string messageBody)
    {
        try
        {
            using var document = JsonDocument.Parse(messageBody);

            if (document.RootElement.TryGetProperty("QueueName", out _))
            {
                return typeof(TestQueueMessage).FullName;
            }

            if (document.RootElement.TryGetProperty("TopicName", out _))
            {
                return typeof(TestTopicMessage).FullName;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static void SetupSendFailure(string entityName, string errorMessage = "Mock service bus error")
    {
        var exception = new ServiceBusException(errorMessage, ServiceBusFailureReason.ServiceTimeout);
        SendFailures[entityName] = exception;
    }

    public static void SetupCreateSenderFailure(string entityName, string errorMessage = "Mock create sender error")
    {
        MockServiceBusClient.CreateSender(entityName)
            .Throws(new ServiceBusException(errorMessage, ServiceBusFailureReason.GeneralError));
    }

    public static void ClearSendFailure(string entityName)
    {
        SendFailures.TryRemove(entityName, out _);
    }

    public static void ClearAllSendFailures()
    {
        SendFailures.Clear();
    }

    public static IReadOnlyList<SentMessage> GetSentMessages()
    {
        return SentMessages.ToList().AsReadOnly();
    }

    public static IEnumerable<SentMessage> GetSentMessagesForEntity(string entityName)
    {
        return SentMessages.Where(m => string.Equals(m.EntityName, entityName, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public static IEnumerable<T> GetSentMessages<T>() where T : class
    {
        var targetTypeName = typeof(T).FullName;

        return SentMessages
            .Where(m => m.ContentType == "application/json")
            .Where(m => m.OriginalMessageType == targetTypeName)
            .Select(m =>
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(m.MessageBody);
                }
                catch
                {
                    return null;
                }
            })
            .Where(m => m != null)
            .Cast<T>()
            .ToList();
    }

    public static IEnumerable<T> GetSentMessagesForEntity<T>(string entityName) where T : class
    {
        var targetTypeName = typeof(T).FullName;

        return SentMessages
            .Where(m => string.Equals(m.EntityName, entityName, StringComparison.OrdinalIgnoreCase))
            .Where(m => m.ContentType == "application/json")
            .Where(m => m.OriginalMessageType == targetTypeName)
            .Select(m =>
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(m.MessageBody);
                }
                catch
                {
                    return null;
                }
            })
            .Where(m => m != null)
            .Cast<T>()
            .ToList();
    }

    public static void ClearSentMessages()
    {
        while (!SentMessages.IsEmpty)
        {
            SentMessages.TryTake(out _);
        }
    }

    public static int GetMessageCountForEntity(string entityName)
    {
        return SentMessages.Count(m => string.Equals(m.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
    }

    public static int TotalSentMessageCount => SentMessages.Count;

    public static void VerifyMessageSent(string entityName, int expectedCount = 1)
    {
        var actualCount = GetMessageCountForEntity(entityName);
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException($"Expected {expectedCount} messages to {entityName}, but found {actualCount}");
        }
    }

    public static void VerifySendMessageAsyncCalled(string entityName, int expectedTimes = 1)
    {
        if (!MockQueueSenders.ContainsKey(entityName) && !MockTopicSenders.ContainsKey(entityName))
        {
            throw new InvalidOperationException($"No sender was created for entity '{entityName}'");
        }
    }

    public static void VerifyCreateSenderCalled(string entityName, int expectedTimes = 1)
    {
        MockServiceBusClient.Received(expectedTimes).CreateSender(entityName);
    }
}
