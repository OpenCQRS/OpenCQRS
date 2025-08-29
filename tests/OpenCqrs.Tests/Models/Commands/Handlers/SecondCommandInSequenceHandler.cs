using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class SecondCommandInSequenceHandler : ICommandHandler<SecondCommandInSequence, string>
{
    public async Task<Result<string>> Handle(SecondCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new Failure();
    }
}
