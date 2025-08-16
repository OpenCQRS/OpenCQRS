using System;
using System.Threading.Tasks;
using OpenCqrs.Bus;
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
        private readonly ICommandSender _commandSender;
        private readonly IEventPublisher _eventPublisher;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IBusMessageDispatcher _busMessageDispatcher;

        public Dispatcher(ICommandSender domainCommandSender,
            IEventPublisher eventPublisher,
            IQueryProcessor queryProcessor,
            IBusMessageDispatcher busMessageDispatcher)
        {
            _commandSender = domainCommandSender;
            _eventPublisher = eventPublisher;
            _queryProcessor = queryProcessor;
            _busMessageDispatcher = busMessageDispatcher;
        }

        /// <inheritdoc />
        public Task SendAsync<TCommand>(TCommand command) 
            where TCommand : ICommand
        {
            return _commandSender.SendAsync(command);
        }

        /// <inheritdoc />
        public Task SendAsync<TCommand>(TCommand command, Func<Task<CommandResponse>> commandHandler) 
            where TCommand : ICommand
        {
            return _commandSender.SendAsync(command, commandHandler);
        }

        /// <inheritdoc />
        public Task SendAsync(ICommandSequence commandSequence)
        {
            return _commandSender.SendAsync(commandSequence);
        }

        /// <inheritdoc />
        public Task<TResult> SendAsync<TResult>(ICommand command)
        {
            return _commandSender.SendAsync<TResult>(command);
        }

        /// <inheritdoc />
        public Task<TResult> SendAsync<TResult>(ICommand command, Func<Task<CommandResponse>> commandHandler)
        {
            return _commandSender.SendAsync<TResult>(command, commandHandler);
        }

        /// <inheritdoc />
        public Task<TResult> SendAsync<TResult>(ICommandSequence commandSequence)
        {
            return _commandSender.SendAsync<TResult>(commandSequence);
        }

        /// <inheritdoc />
        public Task PublishAsync<TEvent>(TEvent @event) 
            where TEvent : IEvent
        {
            return _eventPublisher.PublishAsync(@event);
        }

        /// <inheritdoc />
        public Task<TResult> GetResultAsync<TResult>(IQuery<TResult> query)
        {
            return _queryProcessor.ProcessAsync(query);
        }

        /// <inheritdoc />
        public Task DispatchBusMessageAsync<TMessage>(TMessage message) 
            where TMessage : IBusMessage
        {
            return _busMessageDispatcher.DispatchAsync(message);
        }
    }
}