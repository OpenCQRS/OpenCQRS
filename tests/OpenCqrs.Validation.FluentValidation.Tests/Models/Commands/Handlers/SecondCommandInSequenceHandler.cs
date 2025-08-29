using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Handlers;

public class SecondCommandInSequenceHandler : ICommandSequenceHandler<SecondCommandInSequence, string>
{
    public async Task<Result<string>> Handle(SecondCommandInSequence command, IEnumerable<Result<string>> previousCommandResults, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new Failure();
    }
}
