using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Tests;

// The domain shapes an uploaded assembly is expected to contain. They live here so the scanner can
// be pointed at this test assembly rather than at a fixture compiled at test time.

[EventType("SampleHappened", 1)]
public record SampleHappenedEvent(string Id) : IEvent;

[AggregateType("SampleAggregate", 1)]
public class SampleAggregate : AggregateRoot
{
    public override Type[]? EventTypeFilter => null;

    protected override bool Apply<T>(T @event) => false;
}

public class SampleAggregateId(string id) : IAggregateId<SampleAggregate>
{
    public string Id { get; } = id;

    public IDictionary<string, string>? EventPropertyFilter => null;
}

[ProjectionType("SampleProjection", 1)]
public class SampleProjection : Projection
{
    public override Type[]? EventTypeFilter => null;

    protected override bool Apply<T>(T @event) => false;
}

public class SampleProjectionId(string id) : IProjectionId<SampleProjection>
{
    public string Id { get; } = id;

    public IDictionary<string, string>? EventPropertyFilter => null;
}

[AggregateType("SampleDcbAggregate", 1)]
public class SampleDcbAggregate : DcbAggregateRoot
{
    public override Type[]? EventTypeFilter => [typeof(SampleHappenedEvent)];

    public string Name { get; private set; } = string.Empty;

    protected override bool Apply<T>(T @event) => false;
}

public class SampleDcbAggregateId(string id) : IDcbAggregateId<SampleDcbAggregate>
{
    public string Id { get; } = id;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("sample", id));
}

[ProjectionType("SampleDcbProjection", 1)]
public class SampleDcbProjection : DcbProjection
{
    public override Type[]? EventTypeFilter => null;

    protected override bool Apply<T>(T @event) => false;
}

public class SampleDcbProjectionId(string id) : IDcbProjectionId<SampleDcbProjection>
{
    public string Id { get; } = id;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("sample", id));
}
