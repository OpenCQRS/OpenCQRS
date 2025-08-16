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

public class DomainEventType(string name, byte version = 1) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}
