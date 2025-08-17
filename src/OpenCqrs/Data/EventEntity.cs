using Newtonsoft.Json;
using OpenCqrs.Domain;

namespace OpenCqrs.Data;

public class EventEntity : IAuditableEntity, IBindableEntity
{
    public string Id { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public int Sequence { get; set; }
    public string Data { get; set; } = null!;
    public DateTimeOffset TimeStamp { get; set; }
    public string? UserId { get; set; }
    public string? Source { get; set; }
    
    public DateTimeOffset CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LatestUpdatedDate { get; set; }
    public string? LatestUpdatedBy { get; set; }
    
    public string TypeName { get; set; } = null!;
    public int TypeVersion { get; set; }
}

public static class EventEntityExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };
    
    public static DomainEvent ToDomainEvent(this EventEntity eventEntity)
    {
        var typeFound = TypeBindings.DomainEventBindings.TryGetValue(eventEntity.ToBindingKey(), out var eventType);
        
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {eventEntity.TypeName} not found in TypeBindings");
        }
        
        return (DomainEvent)JsonConvert.DeserializeObject(eventEntity.Data, eventType!, JsonSerializerSettings)!;
    }
}
