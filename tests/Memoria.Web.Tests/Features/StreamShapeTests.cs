using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The pattern a stream type's ids match, worked out by building one from values chosen to be
/// recognisable and reading the id it produces. Nothing declares this — a stream carries no
/// attribute and the log stores the id it made, not the type that made it — so asking the type is
/// the only way to know it for one uploaded after the fact.
/// </summary>
public class StreamShapeTests
{
    /// <summary>
    /// Worked out once and kept, so a test that has already asked is not what the next one reads.
    /// </summary>
    public StreamShapeTests() => StreamShape.Forget();

    /// <summary>
    /// The usual shape: what the stream is, then the value it was named for. The value becomes the
    /// hole and everything the type wrote around it stays put.
    /// </summary>
    [Fact]
    public void Leaves_a_hole_where_the_value_goes()
    {
        StreamShape.Of(typeof(SamplePrefixedStreamId))!.Pattern.Should().Be("sample:%");
    }

    [Fact]
    public void Leaves_a_hole_for_every_value()
    {
        StreamShape.Of(typeof(SampleTwoPartStreamId))!.Pattern.Should().Be("sample:%:%");
    }

    /// <summary>
    /// A stream whose id is the value it was given and nothing else matches anything at all, which
    /// is the truthful answer: it can name every id in the log.
    /// </summary>
    [Fact]
    public void Is_a_bare_wildcard_when_the_id_is_only_the_value()
    {
        StreamShape.Of(typeof(SampleStreamId))!.Pattern.Should().Be("%");
    }

    /// <summary>
    /// Nothing to put in means nothing to leave out: one stream, and the pattern is its id.
    /// </summary>
    [Fact]
    public void Is_the_id_itself_when_the_stream_takes_nothing()
    {
        StreamShape.Of(typeof(SampleOnlyStreamId))!.Pattern.Should().Be("samples");
    }

    /// <summary>
    /// A value nothing can stand in for leaves no way to tell which part of the id came from it, so
    /// there is no shape rather than a guessed one.
    /// </summary>
    [Fact]
    public void Has_no_shape_when_a_value_cannot_be_stood_in_for()
    {
        StreamShape.Of(typeof(SampleUnprobedStreamId)).Should().BeNull();
    }

    /// <summary>
    /// Asked of something that is not a stream at all — an aggregate's identifier names a model
    /// inside a stream, not the stream.
    /// </summary>
    [Fact]
    public void Has_no_shape_for_a_type_that_is_not_a_stream()
    {
        StreamShape.Of(typeof(SampleAggregateId)).Should().BeNull();
    }

    /// <summary>
    /// Probing means building one, so the answer is kept: neither the type nor what it produces
    /// changes while the assemblies are loaded.
    /// </summary>
    [Fact]
    public void Is_the_same_shape_when_asked_again()
    {
        StreamShape.Of(typeof(SamplePrefixedStreamId))
            .Should().BeSameAs(StreamShape.Of(typeof(SamplePrefixedStreamId)));
    }
}
