using OpenCqrs.Commands;
using OpenCqrs.Examples.Messaging.ServiceBus.Messages;
using OpenCqrs.Messaging;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.Messaging.ServiceBus.Commands.Handlers;

public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        // Business logic to place the order would go here.

        var commandResponse = new CommandResponse
        {
            Messages = new  List<Message>
            {
                new TestQueueMessage
                {
                    TestData = "Test Queue Message from PlaceOrderCommandHandler",
                    QueueName = "test-queue"
                },
                new TestTopicMessage
                {
                    TestData = "Test Topic Message from PlaceOrderCommandHandler",
                    TopicName = "test-topic"
                }
            }
        };
        
        return commandResponse;
    }
}
