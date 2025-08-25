using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;

public class OrderAggregateId(Guid orderId) : IAggregateId<OrderAggregate>
{
    public string Id => $"order:{orderId}";
}
