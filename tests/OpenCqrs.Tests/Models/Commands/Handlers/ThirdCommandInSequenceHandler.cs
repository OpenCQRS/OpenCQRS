using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class ThirdCommandInSequenceHandler : ICommandHandler<ThirdCommandInSequence, string>
{
    public async Task<Result<string>> Handle(ThirdCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return "Third command result";
    }
}
