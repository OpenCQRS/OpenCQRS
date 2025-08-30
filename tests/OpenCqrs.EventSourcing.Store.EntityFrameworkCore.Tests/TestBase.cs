using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Aggregates;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Events;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests;

public abstract class TestBase
{
    protected readonly IDomainService DomainService;
    
    protected TestBase()
    {
        TypeBindings.DomainEventTypeBindings = new Dictionary<string, Type>
        {
            {"TestAggregateCreated|v:1", typeof(TestAggregateCreatedEvent)},
            {"TestAggregateUpdated|v:1", typeof(TestAggregateUpdatedEvent)},
            {"SomethingHappened|v:1", typeof(SomethingHappenedEvent)}
        };

        TypeBindings.AggregateTypeBindings = new Dictionary<string, Type>
        {
            {"TestAggregate1|v:1", typeof(TestAggregate1)},
            {"TestAggregate2|v:1", typeof(TestAggregate2)}
        };
        
        var dbContext = Shared.CreateTestDbContext();
        DomainService = new EntityFrameworkCoreDomainService(dbContext);
    }
}
