using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenCqrs.Bus.RabbitMQ.Factories;
using RabbitMQ.Client;

namespace OpenCqrs.Bus.RabbitMQ
{
    public class RabbitMQBusProvider : IBusProvider
    {
        private readonly IMessageFactory _messageFactory;
        private readonly string _connectionString;

        public RabbitMQBusProvider(IMessageFactory messageFactory, IOptions<BusOptions> settings)
        {
            _messageFactory = messageFactory;
            _connectionString = settings.Value.ConnectionString;
        }

        /// <inheritdoc />
        public Task SendQueueMessageAsync<TMessage>(TMessage message) where TMessage : IBusQueueMessage
        {
            if (string.IsNullOrEmpty(message.QueueName))
            {
                throw new ApplicationException("Queue name is mandatory");
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString)
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: message.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

                var body = _messageFactory.CreateMessage(message);

                channel.BasicPublish(exchange: string.Empty, routingKey: message.QueueName, basicProperties: null, body: body);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendTopicMessageAsync<TMessage>(TMessage message) where TMessage : IBusTopicMessage
        {
            if (string.IsNullOrEmpty(message.TopicName))
                throw new ApplicationException("Topic name is mandatory");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString)
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: message.TopicName, type: "fanout");

                var body = _messageFactory.CreateMessage(message);
                var properties = channel.CreateBasicProperties();
                _messageFactory.PopulateProperties(message, properties);
                channel.BasicPublish(exchange: message.TopicName, routingKey: string.Empty, basicProperties: properties, body: body);
            }

            return Task.CompletedTask;
        }
    }
}