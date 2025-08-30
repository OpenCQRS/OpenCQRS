using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class AggregateDocument : DocumentBase, IAuditableDocument, IEditableDocument, IBindableDocument
{
    public string Id { get; set; } = null!;
    
    public int Version { get; set; }

    public int LatestEventSequence { get; set; }

    public string Data { get; set; } = null!;

    public DateTimeOffset CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public string TypeName { get; set; } = null!;

    public int TypeVersion { get; set; }
}

public static class AggregateDocumentExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    public static T ToAggregate<T>(this AggregateDocument aggregateDocument) where T : IAggregate
    {
        var typeFound = TypeBindings.AggregateTypeBindings.TryGetValue(aggregateDocument.GetTypeBindingKey(), out var aggregateType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Aggregate type {aggregateDocument.TypeName} not found in TypeBindings");
        }

        var aggregate = (T)JsonConvert.DeserializeObject(aggregateDocument.Data, aggregateType!, JsonSerializerSettings)!;
        aggregate.StreamId = aggregateDocument.StreamId;
        aggregate.AggregateId = aggregateDocument.Id;
        aggregate.Version = aggregateDocument.Version;
        aggregate.LatestEventSequence = aggregateDocument.LatestEventSequence;
        return aggregate;
    }
}
