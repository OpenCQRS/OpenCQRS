using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;

public class OrderAggregateId(Guid orderId) : IAggregateId<OrderAggregateRoot>
{
    public string Id => $"order:{orderId}";
}
