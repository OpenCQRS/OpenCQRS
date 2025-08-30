using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class AggregateExtensions
{
    public static AggregateDocument ToAggregateDocument(this IAggregate aggregate, IStreamId streamId,
        IAggregateId aggregateId, int newLatestEventSequence, DateTimeOffset timeStamp)
    {
        var eventTypeAttribute = aggregate.GetType().GetCustomAttribute<AggregateType>();
        if (eventTypeAttribute == null)
        {
            throw new InvalidOperationException($"Aggregate {aggregate.GetType().Name} does not have a AggregateType attribute.");
        }

        aggregate.StreamId = streamId.Id;
        aggregate.AggregateId = aggregateId.ToIdWithTypeVersion(eventTypeAttribute.Version);
        aggregate.LatestEventSequence = newLatestEventSequence;

        return new AggregateDocument
        {
            Id = aggregateId.ToIdWithTypeVersion(eventTypeAttribute.Version),
            StreamId = streamId.Id,
            Version = aggregate.Version,
            LatestEventSequence = newLatestEventSequence,
            TypeName = eventTypeAttribute.Name,
            TypeVersion = eventTypeAttribute.Version,
            Data = JsonConvert.SerializeObject(aggregate),
            CreatedDate = timeStamp,
            CreatedBy = null, // TODO: Set created by
            UpdatedDate = timeStamp,
            UpdatedBy = null // TODO: Set updated by
        };
    }
}
