using Newtonsoft.Json;
using OpenCqrs.Domain;

namespace OpenCqrs.Data;

public class AggregateEntity : IAuditableEntity, IBindableEntity
{
    public string Id { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public int Version { get; set; }
    public int LatestEventSequence { get; set; }
    public string Data { get; set; } = null!;
    
    public DateTimeOffset CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LatestUpdatedDate { get; set; }
    public string? LatestUpdatedBy { get; set; }
    
    public string TypeName { get; set; } = null!;
    public int TypeVersion { get; set; }
}

public static class AggregateEntityExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };
    
    public static T ToAggregate<T>(this AggregateEntity aggregateEntity) where T : IAggregate
    {
        var typeFound = TypeBindings.AggregateBindings.TryGetValue(aggregateEntity.ToBindingKey(), out var aggregateType);
        
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Aggregate type {aggregateEntity.TypeName} not found in TypeBindings");
        }
        
        return (T)JsonConvert.DeserializeObject(aggregateEntity.Data, aggregateType!, JsonSerializerSettings)!;
    }
}
