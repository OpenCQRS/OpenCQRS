using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using OpenCqrs.Bus.ServiceBus.Factories;

namespace OpenCqrs.Bus.ServiceBus
{
    public class ServiceBusProvider : IBusProvider
    {
        private readonly IMessageFactory _messageFactory;
        private readonly string _connectionString;

        public ServiceBusProvider(IMessageFactory messageFactory, IOptions<BusOptions> settings)
        {
            _messageFactory = messageFactory;
            _connectionString = settings.Value.ConnectionString;
        }

        /// <inheritdoc />
        public async Task SendQueueMessageAsync<TMessage>(TMessage message) where TMessage : IBusQueueMessage
        {
            if (string.IsNullOrEmpty(message.QueueName))
            {
                throw new ApplicationException("Queue name is mandatory");
            }

            var client = new QueueClient(new ServiceBusConnectionStringBuilder(_connectionString)
            {
                EntityPath = message.QueueName
            });

            var serviceBusMessage = _messageFactory.CreateMessage(message);

            await client.SendAsync(serviceBusMessage);

            await client.CloseAsync();
        }

        /// <inheritdoc />
        public async Task SendTopicMessageAsync<TMessage>(TMessage message) where TMessage : IBusTopicMessage
        {
            if (string.IsNullOrEmpty(message.TopicName))
                throw new ApplicationException("Topic name is mandatory");

            var client = new TopicClient(new ServiceBusConnectionStringBuilder(_connectionString)
            {
                EntityPath = message.TopicName
            });

            var serviceBusMessage = _messageFactory.CreateMessage(message);

            await client.SendAsync(serviceBusMessage);

            await client.CloseAsync();
        }
    }
}