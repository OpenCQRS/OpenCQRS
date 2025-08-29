using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Commands.Handlers;

public class ThirdCommandInSequenceHandler : ICommandSequenceHandler<ThirdCommandInSequence, string>
{
    public async Task<Result<string>> Handle(ThirdCommandInSequence command, IEnumerable<Result<string>> previousCommandResults, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return $"Third command result; {string.Join(", ", previousCommandResults.Select(r => r.Value))}";
    }
}
