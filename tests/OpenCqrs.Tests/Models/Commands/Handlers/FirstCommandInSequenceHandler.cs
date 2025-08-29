using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class FirstCommandInSequenceHandler : ICommandHandler<FirstCommandInSequence, string>
{
    public async Task<Result<string>> Handle(FirstCommandInSequence command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return "First command result";
    }
}
