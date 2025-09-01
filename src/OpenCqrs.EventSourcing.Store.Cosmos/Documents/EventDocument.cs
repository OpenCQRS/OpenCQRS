using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class EventDocument
{
    [JsonProperty("streamId")]
    public string StreamId { get; set; } = null!;

    [JsonProperty("documentType")]
    public static string DocumentType => Documents.DocumentType.Event;

    [JsonProperty("eventType")]
    public string EventType { get; set; } = null!;

    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("sequence")]
    public int Sequence { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; } = null!;

    [JsonProperty("createdDate")]
    public DateTimeOffset CreatedDate { get; set; }

    [JsonProperty("createdBy")]
    public string? CreatedBy { get; set; }
}

public static class EventDocumentExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    public static IDomainEvent ToDomainEvent(this EventDocument eventDocument)
    {
        var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(eventDocument.EventType, out var eventType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {eventDocument.EventType} not found in TypeBindings");
        }

        return (IDomainEvent)JsonConvert.DeserializeObject(eventDocument.Data, eventType!, JsonSerializerSettings)!;
    }
}
