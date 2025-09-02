using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class AggregateExtensions
{
    public static AggregateDocument ToAggregateDocument(this IAggregate aggregate, IStreamId streamId, IAggregateId aggregateId, int newLatestEventSequence)
    {
        var aggregateTypeAttribute = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (aggregateTypeAttribute == null)
        {
            throw new Exception($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version);
        aggregate.LatestEventSequence = newLatestEventSequence;

        return new AggregateDocument
        {
            Id = aggregateId.ToIdWithTypeVersion(aggregateTypeAttribute.Version),
            StreamId = streamId.Id,
            Version = aggregate.Version,
            LatestEventSequence = newLatestEventSequence,
            AggregateType = TypeBindings.GetTypeBindingKey(aggregateTypeAttribute.Name, aggregateTypeAttribute.Version),
            Data = JsonConvert.SerializeObject(aggregate)
        };
    }
}
