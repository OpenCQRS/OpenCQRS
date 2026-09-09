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

/// <summary>
/// A shape holding one of everything, so what is read back off an instance can be checked against
/// every kind of thing a model or an event holds.
/// </summary>
public record SampleHolding(
    string Label,
    SampleMeasurement Measurement,
    IReadOnlyList<string> Notes,
    IReadOnlyList<int> Counts,
    IReadOnlyList<SampleLabel> Labels,
    IReadOnlyList<SampleLabel> Nothing,
    SampleChain Chain,
    SampleMeasurement? Missing);

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

/// <summary>
/// A stream the streamed models are folded from. It carries no attribute and implements nothing
/// the aggregate and projection identifiers implement, so finding it is the scanner's own work.
/// </summary>
/// <remarks>
/// Its id is the value it was given and nothing else, so the pattern it matches by is a wildcard
/// on its own — a stream type that claims every id in the log.
/// </remarks>
public class SampleStreamId(string id) : IStreamId
{
    public string Id { get; } = id;
}

/// <summary>
/// A stream naming what it is before the value it was given, as a stream per customer or per
/// warehouse does. The usual shape, and the one a pattern is worth having for.
/// </summary>
public class SamplePrefixedStreamId(string sampleId) : IStreamId
{
    public string Id => $"sample:{sampleId}";
}

/// <summary>
/// A stream built from two values, so a pattern has more than one hole to leave in it.
/// </summary>
public class SampleTwoPartStreamId(string sampleId, int year) : IStreamId
{
    public string Id => $"sample:{sampleId}:{year}";
}

/// <summary>
/// One stream for everything, taking nothing to name it. Its id is fixed, so the pattern is that
/// id exactly rather than anything with a hole in it.
/// </summary>
public class SampleOnlyStreamId : IStreamId
{
    public string Id => "samples";
}

/// <summary>
/// A stream named from a value nothing can stand in for, so its pattern cannot be worked out: with
/// no recognisable value to put in, there is no way to tell which part of the id came from it.
/// </summary>
public class SampleUnprobedStreamId(SampleMeasurement size) : IStreamId
{
    public string Id => $"sample:{size.Width}";
}

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

/// <summary>
/// A second DCB write model, applying a different event from <see cref="SampleDcbAggregate"/>, so
/// the events one consistency model applies can be seen to be the union across its models rather
/// than whichever one was asked first.
/// </summary>
[AggregateType("SampleCarryingAggregate", 1)]
public class SampleCarryingDcbAggregate : DcbAggregateRoot
{
    public override Type[]? EventTypeFilter => [typeof(SampleCarriedEvent)];

    protected override bool Apply<T>(T @event) => false;
}

/// <summary>
/// An event carrying no <see cref="EventType"/>, so nothing writes it into the log and nothing
/// reads it back. Listed all the same, because a type a model applies is worth seeing whether or
/// not it is bound.
/// </summary>
public record SampleUnboundEvent(string Id) : IEvent;

/// <summary>
/// A DCB write model carrying no <see cref="AggregateType"/>, so the store has no key to write its
/// snapshots under and could never hold one. Listed all the same, because a type the assemblies
/// declare is worth seeing whether or not it is bound.
/// </summary>
public class SampleUnboundDcbAggregate : DcbAggregateRoot
{
    public override Type[]? EventTypeFilter => [typeof(SampleHappenedEvent)];

    protected override bool Apply<T>(T @event) => false;
}

/// <summary>
/// A second version of a name already bound, so a page has two of one name to tell apart — which is
/// the only time the version is worth saying beside it.
/// </summary>
[EventType("SampleRevised", 2)]
public record SampleRevisedEvent(string Id) : IEvent;
