using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class FirstCommandInSequenceHandler : ICommandSequenceHandler<FirstCommandInSequence, string>
{
    public async Task<Result<string>> Handle(FirstCommandInSequence command, IEnumerable<Result<string>> previousResults, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return "First command result";
    }
}
