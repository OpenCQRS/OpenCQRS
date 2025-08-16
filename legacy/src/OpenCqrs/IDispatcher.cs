using System;
using System.Threading.Tasks;
using OpenCqrs.Bus;
using OpenCqrs.Commands;
using OpenCqrs.Events;
using OpenCqrs.Queries;

namespace OpenCqrs
{
    /// <summary>
    /// IDispatcher
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Sends the specified command asynchronously.
        /// The command handler must implement OpenCqrs.Commands.ICommandHandlerAsync&lt;TCommand&gt;.
        /// </summary>
        /// <param name="command">The command.</param>
        Task SendAsync<TCommand>(TCommand command)
            where TCommand : ICommand;

        /// <summary>Sends the specified command asynchronously.</summary>
        /// <param name="command">The command.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <returns></returns>
        Task SendAsync<TCommand>(TCommand command, Func<Task<CommandResponse>> commandHandler)
            where TCommand : ICommand;

        /// <summary>
        /// Sends the specified command sequence asynchronously.
        /// The command handler must implement OpenCqrs.Commands.ISequenceCommandHandlerAsync&lt;TCommand&gt;.
        /// </summary>
        /// <param name="commandSequence">The command sequence.</param>
        Task SendAsync(ICommandSequence commandSequence);

        /// <summary>
        /// Sends the specified command asynchronously.
        /// The command handler must implement OpenCqrs.Commands.ICommandHandlerAsync&lt;TCommand&gt;.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A custom object set as result in the command hadler response.</returns>
        Task<TResult> SendAsync<TResult>(ICommand command);

        /// <summary>Sends the specified command asynchronously.</summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <returns></returns>
        Task<TResult> SendAsync<TResult>(ICommand command, Func<Task<CommandResponse>> commandHandler);

        /// <summary>
        /// Sends the specified command sequence asynchronously.
        /// The command handler must implement OpenCqrs.Commands.ISequenceCommandHandlerAsync&lt;TCommand&gt;.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="commandSequence">The command sequence.</param>
        /// <returns>A custom object set as result in the command hadler response.</returns>
        Task<TResult> SendAsync<TResult>(ICommandSequence commandSequence);

        /// <summary>
        /// Asynchronously publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandlerAsync&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event.</param>
        Task PublishAsync<TEvent>(TEvent @event) 
            where TEvent : IEvent;

        /// <summary>
        /// Asynchronously gets the result.
        /// The query handler must implement OpenCqrs.Queries.IQueryHandlerAsync&lt;TQuery, TResult&gt;.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>TResult</returns>
        Task<TResult> GetResultAsync<TResult>(IQuery<TResult> query);

        /// <summary>
        /// Dispatches the bus message asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task DispatchBusMessageAsync<TMessage>(TMessage message) 
            where TMessage : IBusMessage;

        /// <summary>
        /// Sends the specified command.
        /// The command handler must implement OpenCqrs.Commands.ICommandHandler&lt;TCommand&gt;.
        /// </summary>
        /// <param name="command">The command.</param>
        void Send<TCommand>(TCommand command)
            where TCommand : ICommand;

        /// <summary>Sends the specified command.</summary>
        /// <param name="command">The command.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <returns></returns>
        void Send<TCommand>(TCommand command, Func<CommandResponse> commandHandler)
            where TCommand : ICommand;

        /// <summary>
        /// Sends the specified command sequence.
        /// The command handler must implement OpenCqrs.Commands.ISequenceCommandHandler&lt;TCommand&gt;.
        /// </summary>
        /// <param name="commandSequence">The command sequence.</param>
        void Send(ICommandSequence commandSequence);

        /// <summary>
        /// Sends the specified command.
        /// The command handler must implement OpenCqrs.Commands.ICommandHandler&lt;TCommand&gt;.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A custom object set as result in the command hadler response.</returns>
        TResult Send<TResult>(ICommand command);

        /// <summary>Sends the specified command.</summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <returns></returns>
        TResult Send<TResult>(ICommand command, Func<CommandResponse> commandHandler);

        /// <summary>
        /// Sends the sequence specified command sequence.
        /// The command handler must implement OpenCqrs.Commands.ISequenceCommandHandler&lt;TCommand&gt;.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="commandSequence">The command sequence.</param>
        /// <returns>A custom object set as result in the command hadler response.</returns>
        TResult Send<TResult>(ICommandSequence commandSequence);

        /// <summary>
        /// Publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandler&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event.</param>
        void Publish<TEvent>(TEvent @event) 
            where TEvent : IEvent;

        /// <summary>
        /// Gets the result.
        /// The query handler must implement OpenCqrs.Queries.IQueryHandler&lt;TQuery, TResult&gt;.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>TResult</returns>
        TResult GetResult<TResult>(IQuery<TResult> query);
    }
}
