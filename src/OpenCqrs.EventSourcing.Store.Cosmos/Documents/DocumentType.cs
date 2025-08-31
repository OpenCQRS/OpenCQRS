namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public static class DocumentType
{
    public static string Event => "Event";
    public static string Aggregate => "Aggregate";
    public static string AggregateEvent => "AggregateEvent";
}
