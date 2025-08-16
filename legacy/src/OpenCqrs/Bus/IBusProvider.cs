using System;
using System.Threading.Tasks;

namespace OpenCqrs.Bus
{
    /// <summary>
    /// IBusProvider
    /// </summary>
    public interface IBusProvider
    {
        /// <summary>
        /// Sends the queue message asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task SendQueueMessageAsync<TMessage>(TMessage message) where TMessage : IBusQueueMessage;

        /// <summary>
        /// Sends the topic message asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task SendTopicMessageAsync<TMessage>(TMessage message) where TMessage : IBusTopicMessage;
    }

    public class DefaultBusProvider : IBusProvider
    {
        public Task SendQueueMessageAsync<TMessage>(TMessage message) where TMessage : IBusQueueMessage
        {
            throw new NotImplementedException(Consts.BusRequiredMessage);
        }

        public Task SendTopicMessageAsync<TMessage>(TMessage message) where TMessage : IBusTopicMessage
        {
            throw new NotImplementedException(Consts.BusRequiredMessage);
        }
    }
}
