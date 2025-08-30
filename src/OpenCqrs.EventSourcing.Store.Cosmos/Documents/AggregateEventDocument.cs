using Newtonsoft.Json;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class AggregateEventDocument : DocumentBase, IApplicableDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    [JsonProperty("aggregateId")]
    public string AggregateId { get; set; } = null!;

    [JsonProperty("eventId")]
    public string EventId { get; set; } = null!;

    [JsonProperty("appliedDate")]
    public DateTimeOffset AppliedDate { get; set; }
}
