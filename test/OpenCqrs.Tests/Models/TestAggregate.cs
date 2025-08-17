using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Models;

[AggregateType("TestAggregate")]
public class TestAggregate : Aggregate
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public TestAggregate()
    {
    }

    public TestAggregate(string id, string name, string description)
    {
        Add(new TestDomainEventV1(id, name, description));
    }

    protected override bool Apply<TDomainEvent>(TDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            TestDomainEventV1 testDomainEventV1 => Apply(testDomainEventV1),
            _ => false
        };
    }
    
    private bool Apply(TestDomainEventV1 @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Description = @event.Description;

        return true;
    }
}
