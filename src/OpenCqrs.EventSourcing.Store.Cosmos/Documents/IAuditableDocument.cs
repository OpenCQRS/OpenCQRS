namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public interface IAuditableDocument
{
    DateTimeOffset CreatedDate { get; set; }

    string? CreatedBy { get; set; }
}
