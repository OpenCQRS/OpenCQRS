using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.Data;

namespace OpenCqrs.Domain;

public interface IDomainEvent;

[AttributeUsage(AttributeTargets.Class)]
public class DomainEventType(string name, byte version = 1) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}

public static class IDomainEventExtensions
{
    public static EventEntity ToEventEntity(this IDomainEvent domainEvent, IStreamId streamId, int sequence)
    {
        var domainEventTypeAttribute = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventTypeAttribute == null)
        {
            throw new InvalidOperationException($"Domain event {domainEvent.GetType().Name} does not have a DomainEventType attribute.");
        }
        
        return new EventEntity
        {
            Id = Guid.NewGuid().ToString(),
            StreamId = streamId.Id,
            Sequence = sequence,
            TypeName = domainEventTypeAttribute.Name,
            TypeVersion = domainEventTypeAttribute.Version,
            Data = JsonConvert.SerializeObject(domainEvent),
            TimeStamp = DateTimeOffset.UtcNow // TODO: Use date time provider
        };
    }
}
