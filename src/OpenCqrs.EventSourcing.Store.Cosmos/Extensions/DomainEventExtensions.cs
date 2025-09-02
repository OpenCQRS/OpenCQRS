using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class DomainEventExtensions
{
    public static EventDocument ToEventDocument(this IDomainEvent domainEvent, IStreamId streamId, int sequence)
    {
        var domainEventType = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventType == null)
        {
            throw new Exception($"Domain event {domainEvent.GetType().Name} does not have a DomainEventType attribute.");
        }

        return new EventDocument
        {
            Id = $"{streamId.Id}:{sequence}",
            StreamId = streamId.Id,
            Sequence = sequence,
            EventType = TypeBindings.GetTypeBindingKey(domainEventType.Name, domainEventType.Version),
            Data = JsonConvert.SerializeObject(domainEvent)
        };
    }
}
