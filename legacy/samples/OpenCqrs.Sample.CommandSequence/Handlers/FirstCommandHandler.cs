using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Sample.CommandSequence.Commands;

namespace OpenCqrs.Sample.CommandSequence.Handlers
{
    public class FirstCommandHandler : ISequenceCommandHandlerAsync<FirstCommand>
    {
        public Task<CommandResponse> HandleAsync(FirstCommand command, CommandResponse previousStepResponse)
        {
            Console.WriteLine("Message from first command handler");

            return Task.FromResult(new CommandResponse
            { 
                Result = "First result"
            });
        }
    }
}
