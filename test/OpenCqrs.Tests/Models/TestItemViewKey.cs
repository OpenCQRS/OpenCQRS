using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Models;

public class TestItemViewKey(string testAggregateId) : IAggregateKey<TestAggregate>
{
    public string Id => $"test-aggregate:{testAggregateId}";
    public string[] EventTypeFilter { get; } = 
    [
        "TestDomainEventV1", 
        "TestDomainEventV2"
    ];
}
