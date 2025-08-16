using System;
using Newtonsoft.Json;
using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents.Factories
{
    public class EventDocumentFactory : IEventDocumentFactory
    {
        public EventDocument CreateEvent(IDomainEvent @event, long version)
        {
            return new EventDocument
            {
                Id = Guid.NewGuid().ToString(),
                AggregateId = @event.AggregateRootId.ToString(),
                CommandId = @event.CommandId.ToString(),
                Sequence = version,
                Type = @event.GetType().AssemblyQualifiedName,
                Data = JsonConvert.SerializeObject(@event),
                TimeStamp = @event.TimeStamp,
                UserId = @event.UserId,
                Source = @event.Source
            };
        }
    }
}