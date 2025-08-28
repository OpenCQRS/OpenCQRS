using OpenCqrs.Commands;
using OpenCqrs.Results;

namespace OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Handlers;

public class DoSomethingHandler : ICommandHandler<DoSomething>
{
    public async Task<Result> Handle(DoSomething command, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return Result.Ok();
    }
}
