using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Handlers;

public class FirstCommandInSequenceHandler : ICommandSequenceHandler<FirstCommandInSequence, string>
{
    public async Task<Result<string>> Handle(FirstCommandInSequence command, IEnumerable<Result<string>> previousCommandResults, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return "First command result";
    }
}
