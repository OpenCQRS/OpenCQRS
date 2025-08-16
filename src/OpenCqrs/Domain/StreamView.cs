using System.Reflection;
using Newtonsoft.Json;

namespace OpenCqrs.Domain;

public interface IStreamView
{
    string StreamId { get; set; }
    string ViewId { get; set; }
    int Version { get; set; }
    int LatestEventSequence { get; set; }
    public IEnumerable<IDomainEvent> UncommittedEvents { get; }
    void ApplyEvents(IEnumerable<IDomainEvent> domainEvents);
}

public abstract class StreamView : IStreamView
{
    public string StreamId { get; set; } = null!;
    public string ViewId { get; set; } = null!;
    public int Version { get; set; }
    public int LatestEventSequence { get; set; }

    [JsonIgnore]
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    [JsonIgnore]
    public IEnumerable<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    protected void AddEvent(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent);

        if (ApplyEvent(domainEvent))
        {
            Version++;
        }
    }
    
    public void ApplyEvents(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            if (ApplyEvent(domainEvent))
            {
                Version++;
            }
        }
    }

    protected abstract bool ApplyEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;
}

public class StreamViewType(string name, byte version = 1) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}

public static class StreamViewExtensions
{
    public static StreamViewType StreamViewType(this IStreamView streamView)
    {
        var streamViewType = streamView.GetType().GetCustomAttribute<StreamViewType>();
        if (streamViewType == null)
        {
            throw new InvalidOperationException($"Stream view {streamView.GetType().Name} does not have a StreamViewType attribute.");
        }
        return streamViewType;
    }
}
