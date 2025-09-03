using FluentAssertions;
using OpenCqrs.Messaging.ServiceBus.Tests.Models;
using Xunit;

namespace OpenCqrs.Messaging.ServiceBus.Tests.Features;

public class ServiceBusMessagingProviderTests
{
    private readonly MockServiceBusTestHelper _mockHelper = new();

    [Fact]
    public async Task SendQueueMessage_WithValidMessage_ShouldSucceed()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage
        {
            QueueName = "test-queue",
            TestData = "Test message data",
            Properties = new Dictionary<string, object> { { "key1", "value1" } }
        };

        // Act
        var result = await provider.SendQueueMessage(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify message was captured
        var sentMessages = _mockHelper.GetSentMessages();
        sentMessages.Should().HaveCount(1);

        var sentMessage = sentMessages.First();
        sentMessage.EntityName.Should().Be("test-queue");
        sentMessage.ContentType.Should().Be("application/json");
        sentMessage.MessageId.Should().NotBeNullOrEmpty();

        // Verify the actual message content
        var deserializedMessages = _mockHelper.GetSentMessages<TestQueueMessage>();
        deserializedMessages.Should().HaveCount(1);

        var deserializedMessage = deserializedMessages.First();
        deserializedMessage.QueueName.Should().Be("test-queue");
        deserializedMessage.TestData.Should().Be("Test message data");
    }

    [Fact]
    public async Task SendTopicMessage_WithValidMessage_ShouldSucceed()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestTopicMessage
        {
            TopicName = "test-topic",
            TestData = "Test topic message",
            Properties = new Dictionary<string, object>
            {
                { "MessageType", "TestEvent" },
                { "Version", 1 }
            }
        };

        // Act
        var result = await provider.SendTopicMessage(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify message was sent to correct topic
        var topicMessages = _mockHelper.GetSentMessagesForEntity("test-topic");
        topicMessages.Should().HaveCount(1);

        // Verify message count helper
        _mockHelper.GetMessageCountForEntity("test-topic").Should().Be(1);
        _mockHelper.TotalSentMessageCount.Should().Be(1);
    }

    [Fact]
    public async Task SendQueueMessage_WithEmptyQueueName_ShouldReturnFailure()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage
        {
            QueueName = string.Empty,
            TestData = "Test data"
        };

        // Act
        var result = await provider.SendQueueMessage(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Title.Should().Be("Queue name");
        result.Failure.Description.Should().Be("Queue name cannot be null or empty");

        // No messages should have been sent
        _mockHelper.TotalSentMessageCount.Should().Be(0);
    }

    [Fact]
    public async Task SendTopicMessage_WithEmptyTopicName_ShouldReturnFailure()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestTopicMessage
        {
            TopicName = string.Empty,
            TestData = "Test data"
        };

        // Act
        var result = await provider.SendTopicMessage(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Title.Should().Be("Topic name");
        result.Failure.Description.Should().Be("Topic name cannot be null or empty");
    }

    [Fact]
    public async Task SendQueueMessage_WithScheduledEnqueueTime_ShouldSetScheduledTime()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var scheduledTime = DateTime.UtcNow.AddHours(1);
        var message = new TestQueueMessage
        {
            QueueName = "scheduled-queue",
            TestData = "Scheduled message",
            ScheduledEnqueueTimeUtc = scheduledTime
        };

        // Act
        var result = await provider.SendQueueMessage(message);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var sentMessages = _mockHelper.GetSentMessages();
        sentMessages.Should().HaveCount(1);

        var sentMessage = sentMessages.First();
        sentMessage.ScheduledEnqueueTime.Should().NotBeNull();
        sentMessage.ScheduledEnqueueTime!.Value.DateTime.Should().BeCloseTo(scheduledTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SendQueueMessage_WithApplicationProperties_ShouldIncludeProperties()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage
        {
            QueueName = "properties-queue",
            TestData = "Message with properties",
            Properties = new Dictionary<string, object>
            {
                { "CustomProperty1", "Value1" },
                { "CustomProperty2", 42 },
                { "CustomProperty3", true }
            }
        };

        // Act
        var result = await provider.SendQueueMessage(message);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var sentMessages = _mockHelper.GetSentMessages();
        var sentMessage = sentMessages.First();

        sentMessage.ApplicationProperties.Should().HaveCount(3);
        sentMessage.ApplicationProperties["CustomProperty1"].Should().Be("Value1");
        sentMessage.ApplicationProperties["CustomProperty2"].Should().Be(42);
        sentMessage.ApplicationProperties["CustomProperty3"].Should().Be(true);
    }

    [Fact]
    public async Task SendQueueMessage_WhenSenderThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage
        {
            QueueName = "error-queue",
            TestData = "This will fail"
        };

        // Setup the mock to throw an exception
        _mockHelper.SetupSendFailure("error-queue", "Mock service bus error");

        // Act
        var result = await provider.SendQueueMessage(message);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Title.Should().Be("Error");
        result.Failure.Description.Should().Be("There was an error when processing the request");
    }

    [Fact]
    public async Task SendMultipleMessages_ShouldCaptureAllMessages()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var queueMessage1 = new TestQueueMessage { QueueName = "queue1", TestData = "Queue message 1" };
        var queueMessage2 = new TestQueueMessage { QueueName = "queue2", TestData = "Queue message 2" };
        var topicMessage = new TestTopicMessage { TopicName = "topic1", TestData = "Topic message 1" };

        // Act
        await provider.SendQueueMessage(queueMessage1);
        await provider.SendQueueMessage(queueMessage2);
        await provider.SendTopicMessage(topicMessage);

        // Assert
        _mockHelper.TotalSentMessageCount.Should().Be(3);
        _mockHelper.GetMessageCountForEntity("queue1").Should().Be(1);
        _mockHelper.GetMessageCountForEntity("queue2").Should().Be(1);
        _mockHelper.GetMessageCountForEntity("topic1").Should().Be(1);

        var allQueueMessages = _mockHelper.GetSentMessages<TestQueueMessage>();
        allQueueMessages.Should().HaveCount(2);

        var allTopicMessages = _mockHelper.GetSentMessages<TestTopicMessage>();
        allTopicMessages.Should().HaveCount(1);
    }

    [Fact]
    public void ClearSentMessages_ShouldRemoveAllCapturedMessages()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage { QueueName = "test-queue", TestData = "Test" };

        // Act - send a message first
        provider.SendQueueMessage(message).Wait();
        _mockHelper.TotalSentMessageCount.Should().Be(1);

        // Clear and verify
        _mockHelper.ClearSentMessages();

        // Assert
        _mockHelper.TotalSentMessageCount.Should().Be(0);
        _mockHelper.GetSentMessages().Should().BeEmpty();
    }

    [Fact]
    public void VerifyMessageSent_WithCorrectCount_ShouldNotThrow()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage { QueueName = "verify-queue", TestData = "Verify test" };

        // Act
        provider.SendQueueMessage(message).Wait();

        // Assert - should not throw
        _mockHelper.VerifyMessageSent("verify-queue", 1);
    }

    [Fact]
    public void VerifyMessageSent_WithIncorrectCount_ShouldThrow()
    {
        // Arrange
        var provider = CreateTestableServiceBusMessagingProvider();
        var message = new TestQueueMessage { QueueName = "verify-queue", TestData = "Verify test" };

        // Act
        provider.SendQueueMessage(message).Wait();

        // Assert
        var action = () => _mockHelper.VerifyMessageSent("verify-queue", 2);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Expected 2 messages to verify-queue, but found 1");
    }

    /// <summary>
    /// Creates a testable ServiceBusMessagingProvider instance.
    /// Note: This is a simplified approach. In practice, you might need to modify
    /// ServiceBusMessagingProvider to accept an injected ServiceBusClient or use a factory pattern.
    /// </summary>
    private ServiceBusMessagingProvider CreateTestableServiceBusMessagingProvider()
    {
        // For this test, we'll use the real ServiceBusMessagingProvider with mock options
        // In a real implementation, you'd want to modify ServiceBusMessagingProvider to accept
        // an injected ServiceBusClient for better testability
        return new ServiceBusMessagingProvider(_mockHelper.MockServiceBusClient);
    }
}
