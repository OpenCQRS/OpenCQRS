using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.Data;

namespace OpenCqrs.Domain;

public interface IAggregate
{
    string StreamId { get; set; }
    string AggregateId { get; set; }
    int Version { get; set; }
    int LatestEventSequence { get; set; }
    public IEnumerable<IDomainEvent> UncommittedEvents { get; }
    void Apply(IEnumerable<IDomainEvent> domainEvents);
}

public abstract class Aggregate : IAggregate
{
    public string StreamId { get; set; } = null!;
    public string AggregateId { get; set; } = null!;
    public int Version { get; set; }
    public int LatestEventSequence { get; set; }

    [JsonIgnore]
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    [JsonIgnore]
    public IEnumerable<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    protected void Add(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent);

        if (Apply(domainEvent))
        {
            Version++;
        }
    }
    
    public void Apply(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            if (Apply(domainEvent))
            {
                Version++;
            }
        }
    }

    protected abstract bool Apply<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;
}

[AttributeUsage(AttributeTargets.Class)]
public class AggregateType(string name, byte version = 1) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}

public static class AggregateExtensions
{
    public static AggregateType AggregateType(this IAggregate aggregate)
    {
        var aggregateType = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateType == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have an AggregateType attribute.");
        }
        return aggregateType;
    }
    
    public static AggregateEntity ToAggregateEntity(this IAggregate aggregate, IStreamId streamId, IAggregateKey aggregateKey, int aggregateVersion, int latestEventSequence)
    {
        var eventTypeAttribute = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (eventTypeAttribute == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }
        
        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateKey.Id;
        aggregate.LatestEventSequence = latestEventSequence;
        
        return new AggregateEntity
        {
            Id = aggregateKey.ToDatabaseId(eventTypeAttribute.Version),
            StreamId = streamId.Id,
            Version = aggregateVersion,
            LatestEventSequence = latestEventSequence,
            TypeName = eventTypeAttribute.Name,
            TypeVersion = eventTypeAttribute.Version,
            Data = JsonConvert.SerializeObject(aggregate)
        };
    }
}
