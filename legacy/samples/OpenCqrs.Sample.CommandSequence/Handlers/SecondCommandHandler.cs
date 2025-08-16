using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Sample.CommandSequence.Commands;

namespace OpenCqrs.Sample.CommandSequence.Handlers
{
    public class SecondCommandHandler : ISequenceCommandHandlerAsync<SecondCommand>
    {
        public Task<CommandResponse> HandleAsync(SecondCommand command, CommandResponse previousStepResponse)
        {
            Console.WriteLine($"Message from second command handler. Result from first handler: {previousStepResponse.Result}");

            return Task.FromResult(new CommandResponse
            {
                Result = "Second result"
            });
        }
    }
}
