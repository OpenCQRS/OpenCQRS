using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class AggregateDocument : IBindableDocument
{
    [JsonProperty("streamId")]
    public string StreamId { get; set; } = null!;

    [JsonProperty("documentType")]
    public static string DocumentType => Documents.DocumentType.Aggregate;

    [JsonProperty("aggregateType")]
    public string AggregateType { get; set; } = null!;
    
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("latestEventSequence")]
    public int LatestEventSequence { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; } = null!;

    [JsonProperty("createdDate")]
    public DateTimeOffset CreatedDate { get; set; }

    [JsonProperty("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonProperty("updatedDate")]
    public DateTimeOffset UpdatedDate { get; set; }

    [JsonProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}

public static class AggregateDocumentExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    public static T ToAggregate<T>(this AggregateDocument aggregateDocument) where T : IAggregate
    {
        var typeFound = TypeBindings.AggregateTypeBindings.TryGetValue(aggregateDocument.AggregateType, out var aggregateType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Aggregate type {aggregateDocument.AggregateType} not found in TypeBindings");
        }

        var aggregate = (T)JsonConvert.DeserializeObject(aggregateDocument.Data, aggregateType!, JsonSerializerSettings)!;
        aggregate.StreamId = aggregateDocument.StreamId;
        aggregate.AggregateId = aggregateDocument.Id;
        aggregate.Version = aggregateDocument.Version;
        aggregate.LatestEventSequence = aggregateDocument.LatestEventSequence;
        return aggregate;
    }
}
