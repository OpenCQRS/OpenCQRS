using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCqrs.Bus;
using OpenCqrs.Dependencies;

namespace OpenCqrs.Events
{
    /// <inheritdoc />
    public class EventPublisher : IEventPublisher
    {
        private readonly IResolver _resolver;
        private readonly IBusMessageDispatcher _busMessageDispatcher;

        public EventPublisher(IResolver resolver, IBusMessageDispatcher busMessageDispatcher)
        {
            _resolver = resolver;
            _busMessageDispatcher = busMessageDispatcher;
        }

        /// <inheritdoc />
        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var handlers = _resolver.ResolveAll<IEventHandlerAsync<TEvent>>();

            foreach (var handler in handlers)
                await handler.HandleAsync(@event);

            if (@event is IBusMessage message)
                await _busMessageDispatcher.DispatchAsync(message);
        }

        /// <inheritdoc />
        public async Task PublishAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent
        {
            foreach (var @event in events)
            {
                await PublishAsync(@event);
            }
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var handlers = _resolver.ResolveAll<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
                handler.Handle(@event);

            if (@event is IBusMessage message)
                _busMessageDispatcher.DispatchAsync(message).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void Publish<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent
        {
            foreach (var @event in events)
            {
                Publish(@event);
            }
        }
    }
}
