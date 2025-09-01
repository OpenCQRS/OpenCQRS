using Newtonsoft.Json;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class AggregateEventDocument
{
    [JsonProperty("streamId")]
    public string StreamId { get; set; } = null!;

    [JsonProperty("documentType")]
    public static string DocumentType => Documents.DocumentType.AggregateEvent;

    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("aggregateId")]
    public string AggregateId { get; set; } = null!;

    [JsonProperty("eventId")]
    public string EventId { get; set; } = null!;

    [JsonProperty("appliedDate")]
    public DateTimeOffset AppliedDate { get; set; }
}
