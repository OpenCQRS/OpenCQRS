namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public abstract class DocumentBase
{
    public string StreamId { get; set; } = null!;
    public string Type { get; set; } = null!;
}
