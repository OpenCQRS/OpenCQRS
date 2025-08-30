namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public interface IEditableDocument
{
    DateTimeOffset UpdatedDate { get; set; }

    string? UpdatedBy { get; set; }
}
