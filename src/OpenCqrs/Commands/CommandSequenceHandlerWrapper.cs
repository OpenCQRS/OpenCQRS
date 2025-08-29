﻿using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Results;

namespace OpenCqrs.Commands;

internal class CommandSequenceHandlerWrapper<TCommand, TResponse> : CommandSequenceHandlerWrapperBase<TResponse> where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> Handle(ICommand<TResponse> command, IEnumerable<Result<TResponse>> previousCommandResults, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = GetHandler<ICommandSequenceHandler<TCommand, TResponse>>(serviceProvider);
        if (handler == null)
        {
            throw new Exception($"Command sequence handler for {typeof(ICommand<TResponse>).Name} not found.");
        }

        return await handler.Handle((TCommand)command, previousCommandResults, cancellationToken);
    }
}

internal abstract class CommandSequenceHandlerWrapperBase<TResponse>
{
    protected static THandler? GetHandler<THandler>(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<THandler>();
    }

    public abstract Task<Result<TResponse>> Handle(ICommand<TResponse> command, IEnumerable<Result<TResponse>> previousCommandResults, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
