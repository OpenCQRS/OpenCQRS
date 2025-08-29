using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class FirstCommandInSequenceHandler : ICommandHandler<FirstCommandInSequence>
{
    public async Task<Result> Handle(FirstCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return Result.Ok();
    }
}
