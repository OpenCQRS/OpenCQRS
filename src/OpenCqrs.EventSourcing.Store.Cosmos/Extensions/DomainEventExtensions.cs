using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class DomainEventExtensions
{
    public static EventDocument ToEventDocument(this IDomainEvent domainEvent, IStreamId streamId, int sequence, DateTimeOffset timeStamp)
    {
        var domainEventTypeAttribute = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventTypeAttribute == null)
        {
            throw new InvalidOperationException($"Domain event {domainEvent.GetType().Name} does not have a DomainEventType attribute.");
        }

        return new EventDocument
        {
            Id = Guid.NewGuid().ToString(),
            StreamId = streamId.Id,
            Sequence = sequence,
            TypeName = domainEventTypeAttribute.Name,
            TypeVersion = domainEventTypeAttribute.Version,
            Data = JsonConvert.SerializeObject(domainEvent),
            CreatedDate = timeStamp,
            CreatedBy = null // TODO: Set created by
        };
    }
}
