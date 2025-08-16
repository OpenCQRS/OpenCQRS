using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenCqrs.Events
{
    /// <summary>
    /// IEventPublisher
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Asynchronously publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandlerAsync&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event.</param>
        Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : IEvent;

        /// <summary>
        /// Asynchronously publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandlerAsync&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="events">The events.</param>
        Task PublishAsync<TEvent>(IEnumerable<TEvent> events)
            where TEvent : IEvent;

        /// <summary>
        /// Publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandler&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event.</param>
        void Publish<TEvent>(TEvent @event) 
            where TEvent : IEvent;

        /// <summary>
        /// Publishes the specified event.
        /// The event handler must implement OpenCqrs.Events.IEventHandler&lt;TEvent&gt;.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="events">The events.</param>
        void Publish<TEvent>(IEnumerable<TEvent> events)
            where TEvent : IEvent;
    }
}
