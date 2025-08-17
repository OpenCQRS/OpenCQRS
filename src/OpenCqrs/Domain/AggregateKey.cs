namespace OpenCqrs.Domain;

public interface IAggregateKey
{
    string Id { get; }
    string[] EventTypeFilter { get; }
}

public interface IAggregateKey<TAggregate> : IAggregateKey where TAggregate : IAggregate;

public static class AggregateKeyExtensions
{
    public static string ToDatabaseId(this IAggregateKey aggregateKey, byte version) => 
        $"{aggregateKey.Id}|v:{version}";

    public static string ToPreviousDatabaseId(this IAggregateKey aggregateKey, byte version) => 
        $"{aggregateKey.Id}|v:{version - 1}";
}
