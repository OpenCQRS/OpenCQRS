using Newtonsoft.Json;
using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Sql.Documents.Factories
{
    public class EventDocumentFactory : IEventDocumentFactory
    {
        public EventDocument CreateEvent(IDomainEvent @event, int version)
        {
            return new EventDocument
            {
                Id = @event.Id,
                AggregateId = @event.AggregateRootId,
                CommandId = @event.CommandId,
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