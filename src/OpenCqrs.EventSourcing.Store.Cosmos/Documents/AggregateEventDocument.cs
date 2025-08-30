namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class AggregateEventDocument : DocumentBase, IApplicableDocument
{
    public string AggregateId { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public DateTimeOffset AppliedDate { get; set; }
}
