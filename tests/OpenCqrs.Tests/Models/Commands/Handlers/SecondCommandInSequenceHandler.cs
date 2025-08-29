using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class SecondCommandInSequenceHandler : ICommandHandler<SecondCommandInSequence>
{
    public async Task<Result> Handle(SecondCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return Result.Fail();
    }
}
