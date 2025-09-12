namespace OpenCqrs.EventSourcing.Configuration;

public class EventSourcingOptions
{
    public bool StoreAggregateSnapshotByDefault { get; set; } = false;

    public bool CheckForNewEventsByDefault { get; set; } = false;
}
