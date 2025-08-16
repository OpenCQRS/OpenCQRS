using System;
using OpenCqrs.Commands;
using OpenCqrs.Events;
using OpenCqrs.Queries;

namespace OpenCqrs
{
    /// <inheritdoc />
    /// <summary>
    /// Dispatcher
    /// </summary>
    /// <seealso cref="T:OpenCqrs.IDispatcher" />
    public partial class Dispatcher : IDispatcher
    {
        /// <inheritdoc />
        public void Send<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            _commandSender.Send(command);
        }

        /// <inheritdoc />
        public void Send<TCommand>(TCommand command, Func<CommandResponse> commandHandler)
            where TCommand : ICommand
        {
            _commandSender.Send(command, commandHandler);
        }

        /// <inheritdoc />
        public void Send(ICommandSequence commandSequence)
        {
            _commandSender.Send(commandSequence);
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommand command)
        {
            return _commandSender.Send<TResult>(command);
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommand command, Func<CommandResponse> commandHandler)
        {
            return _commandSender.Send<TResult>(command, commandHandler);
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommandSequence commandSequence)
        {
            return _commandSender.Send<TResult>(commandSequence);
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent @event) 
            where TEvent : IEvent
        {
            _eventPublisher.Publish(@event);
        }

        /// <inheritdoc />
        public TResult GetResult<TResult>(IQuery<TResult> query)
        {
            return _queryProcessor.Process(query);
        }
    }
}