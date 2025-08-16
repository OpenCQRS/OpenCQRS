using RabbitMQ.Client;

namespace OpenCqrs.Bus.RabbitMQ.Factories
{
    /// <summary>
    /// IMessageFactory
    /// </summary>
    public interface IMessageFactory
    {
        /// <summary>
        /// Creates the RabbitMQ message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        byte[] CreateMessage<TMessage>(TMessage message) where TMessage : IBusMessage;
        void PopulateProperties<TMessage>(TMessage message, IBasicProperties properties) where TMessage : IBusTopicMessage;
    }
}
