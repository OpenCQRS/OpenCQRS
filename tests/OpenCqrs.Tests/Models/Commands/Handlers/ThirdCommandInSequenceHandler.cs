using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class ThirdCommandInSequenceHandler : ICommandHandler<ThirdCommandInSequence>
{
    public async Task<Result> Handle(ThirdCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return Result.Ok();
    }
}
