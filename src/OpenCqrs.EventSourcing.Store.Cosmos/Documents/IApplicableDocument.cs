namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public interface IApplicableDocument
{
    DateTimeOffset AppliedDate { get; set; }
}
