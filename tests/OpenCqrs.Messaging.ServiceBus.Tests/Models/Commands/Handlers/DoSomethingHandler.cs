using OpenCqrs.Commands;
using OpenCqrs.Messaging.ServiceBus.Tests.Models.Messages;
using OpenCqrs.Results;

namespace OpenCqrs.Messaging.ServiceBus.Tests.Models.Commands.Handlers;

public class DoSomethingHandler : ICommandHandler<DoSomething, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(DoSomething command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var queueMessage = new TestQueueMessage
        {
            TestData = command.Name,
            QueueName = "test-queue",
            Properties = new Dictionary<string, object> { { "CommandId", command.Id } }
        };

        var response = new CommandResponse(
            queueMessage,
            new { Message = $"Successfully processed command for: {command.Name}" }
        );

        return response;
    }
}
