using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;

[AggregateType("TestAggregate2")]
public class TestAggregate2 : Aggregate
{
    public override Type[] EventTypeFilter { get; } =
    [
        typeof(TestAggregateCreatedEvent),
        typeof(TestAggregateUpdatedEvent)
    ];

    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public TestAggregate2()
    {
    }

    public TestAggregate2(string id, string name, string description)
    {
        Add(new TestAggregateCreatedEvent(id, name, description));
    }

    public void Update(string name, string description)
    {
        Add(new TestAggregateUpdatedEvent(Id, name, description));
    }

    protected override bool Apply<TDomainEvent>(TDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            TestAggregateCreatedEvent @event => Apply(@event),
            TestAggregateUpdatedEvent @event => Apply(@event),
            _ => false
        };
    }

    private bool Apply(TestAggregateCreatedEvent @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Description = @event.Description;

        return true;
    }

    private bool Apply(TestAggregateUpdatedEvent @event)
    {
        Name = @event.Name;
        Description = @event.Description;

        return true;
    }
}
