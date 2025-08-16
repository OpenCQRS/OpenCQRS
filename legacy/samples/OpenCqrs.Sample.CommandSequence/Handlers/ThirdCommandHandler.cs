using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Sample.CommandSequence.Commands;

namespace OpenCqrs.Sample.CommandSequence.Handlers
{
    public class ThirdCommandHandler : ISequenceCommandHandlerAsync<ThirdCommand>
    {
        public Task<CommandResponse> HandleAsync(ThirdCommand command, CommandResponse previousStepResponse)
        {
            Console.WriteLine($"Message from third command handler. Result from second handler: {previousStepResponse.Result}");

            return Task.FromResult(new CommandResponse
            {
                Result = "Third result"
            });
        }
    }
}
