using System;
using System.Threading.Tasks;

namespace OpenCqrs.Bus
{
    public class BusMessageDispatcher : IBusMessageDispatcher
    {
        private readonly IBusProvider _busProvider;

        public BusMessageDispatcher(IBusProvider busProvider)
        {
            _busProvider = busProvider;
        }

        public Task DispatchAsync<TMessage>(TMessage message) where TMessage : IBusMessage
        {
            if (message is IBusQueueMessage && message is IBusTopicMessage)
            {
                throw new NotSupportedException("The message cannot implement both the IBusQueueMessage and the IBusTopicMessage interfaces");
            }

            if (message is IBusQueueMessage queueMessage)
            {
                return _busProvider.SendQueueMessageAsync(queueMessage);
            }

            if (message is IBusTopicMessage topicMessage)
            {
                return _busProvider.SendTopicMessageAsync(topicMessage);
            }

            throw new NotSupportedException("The message must implement either the IBusQueueMessage or the IBusTopicMessage interface");
        }
    }
}
