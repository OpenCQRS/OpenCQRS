using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.Data;

namespace OpenCqrs.Domain;

public interface IDomainEvent
{
    string EventId { get; set; }
    string StreamId { get; set; }
    DateTimeOffset TimeStamp { get; set; }
    string? UserId { get; set; }
    string? Source { get; set; }
}

public abstract class DomainEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string StreamId { get; set; } = null!;
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string? Source { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class DomainEventType(string name, byte version = 1) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}

public static class DomainEventExtensions
{
    public static EventEntity ToEventEntity(this IDomainEvent domainEvent, IStreamId streamId, int sequence)
    {
        var domainEventTypeAttribute = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventTypeAttribute == null)
        {
            throw new InvalidOperationException($"Domain event {domainEvent.GetType().Name} does not have a DomainEventType attribute.");
        }
        
        domainEvent.StreamId = streamId.Id;
        
        return new EventEntity
        {
            Id = domainEvent.EventId,
            StreamId = streamId.Id,
            Sequence = sequence,
            TypeName = domainEventTypeAttribute.Name,
            TypeVersion = domainEventTypeAttribute.Version,
            Data = JsonConvert.SerializeObject(domainEvent),
            TimeStamp = domainEvent.TimeStamp,
            UserId = domainEvent.UserId,
            Source = domainEvent.Source
        };
    }
}
