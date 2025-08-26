using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.DomainEvents;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;

[AggregateType("Order")]
public class OrderAggregate : Aggregate
{
    public override Type[] EventTypeFilter { get; } =
    [
        typeof(OrderPlacedEvent)
    ];

    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }

    public OrderAggregate() { }

    public OrderAggregate(Guid orderId, decimal amount)
    {
        Add(new OrderPlacedEvent(OrderId = orderId, Amount = amount));
    }

    protected override bool Apply<TDomainEvent>(TDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderPlacedEvent @event => Apply(@event),
            _ => false
        };
    }

    private bool Apply(OrderPlacedEvent @event)
    {
        OrderId = @event.OrderId;
        Amount = @event.Amount;

        return true;
    }
}
