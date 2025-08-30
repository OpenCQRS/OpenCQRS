using Newtonsoft.Json;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public abstract class DocumentBase
{
    [JsonProperty("streamId")]
    public string StreamId { get; set; } = null!;
    
    [JsonProperty("type")]
    public string Type { get; set; } = null!;
}
