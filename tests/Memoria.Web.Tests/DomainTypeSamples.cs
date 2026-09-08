using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Tests;

// The domain shapes an uploaded assembly is expected to contain. They live here so the scanner can
// be pointed at this test assembly rather than at a fixture compiled at test time.

[EventType("SampleHappened", 1)]
public record SampleHappenedEvent(string Id) : IEvent;

/// <summary>
/// A value an event carries whole, rather than as loose numbers beside each other.
/// </summary>
public record SampleMeasurement(decimal Width, decimal Height);

/// <summary>
/// A value holding another value, so there is a second level under it to unfold.
/// </summary>
public record SampleLabel(string Text, SampleMeasurement Size);

/// <summary>
/// A value that holds another of its own kind, which anything unfolding it has to stop on.
/// </summary>
public record SampleChain(string Name, SampleChain? Next);

/// <summary>Where a sample got to, so an enum is among the shapes described.</summary>
public enum SampleState
{
    None = 0,
    Open = 1
}

/// <summary>
/// An event carrying every shape a page has to tell apart: plain values, a value of its own, a list
/// of plain values, a list of values of its own, an enum, and a framework type.
/// </summary>
[EventType("SampleCarried", 1)]
public record SampleCarriedEvent(
    string Id,
    SampleMeasurement Measurement,
    IReadOnlyList<string> Notes,
    IReadOnlyList<SampleLabel> Labels,
    SampleChain Chain,
    SampleState State,
    DateTimeOffset OccurredOn) : IEvent;

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

    /// <summary>A value the model holds whole, as a write model folded from an event would.</summary>
    public SampleMeasurement Measurement { get; private set; } = new(0, 0);

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

/// <summary>
/// An identifier whose boundary is built from two of its values, like a product's id and the sku
/// it has to be unique against.
/// </summary>
public class SampleTwoPartId(string id, string label) : IDcbAggregateId<SampleDcbAggregate>
{
    public string Id { get; } = id;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("sample", id), new Tag("label", label));
}

/// <summary>
/// An identifier taking a value its boundary never mentions, so what exists cannot be worked out
/// from the tags alone.
/// </summary>
public class SampleUnmappedId(string id, string note) : IDcbAggregateId<SampleDcbAggregate>
{
    public string Id { get; } = id;

    public string Note { get; } = note;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("sample", id));
}

/// <summary>
/// An identifier whose boundary is an intersection: only the events carrying both of its tags, like
/// asking whether this one student is already on this one course.
/// </summary>
public class SampleAllOfId(string id, string label) : IDcbAggregateId<SampleDcbAggregate>
{
    public string Id { get; } = id;

    public TagQuery Boundary { get; } = TagQuery.AllOf(new Tag("sample", id), new Tag("label", label));
}
