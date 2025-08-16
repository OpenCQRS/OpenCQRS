using System;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace OpenCqrs.Bus.RabbitMQ.Factories
{
    public class MessageFactory : IMessageFactory
    {
        public static readonly string AssemblyQualifiedNamePropertyName = "AssemblyQualifiedName";

        /// <inheritdoc />
        public byte[] CreateMessage<TMessage>(TMessage message) where TMessage : IBusMessage
        {
            var json = JsonConvert.SerializeObject(message);
            var result = Encoding.UTF8.GetBytes(json);
            return result;
        }

        public void PopulateProperties<TMessage>(TMessage message, IBasicProperties properties) where TMessage : IBusTopicMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            properties.ContentType = "application/json";

            if (message.Properties != null)
            {
                foreach (var prop in message.Properties)
                {
                    properties.Headers.Add(prop.Key, prop.Value);
                }
            }

            properties.Headers.Add(AssemblyQualifiedNamePropertyName, message.GetType().AssemblyQualifiedName);
        }
    }
}