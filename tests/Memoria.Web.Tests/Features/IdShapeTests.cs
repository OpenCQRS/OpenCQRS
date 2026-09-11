using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The pattern the ids of one type match, worked out by building one from values chosen to be
/// recognisable and reading the id it produces. Nothing declares this — a stream and an identifier
/// each carry no attribute, and the store keeps the id that was made rather than the type that made
/// it — so asking the type is the only way to know it for one uploaded after the fact.
/// </summary>
public class IdShapeTests
{
    /// <summary>
    /// Worked out once and kept, so a test that has already asked is not what the next one reads.
    /// </summary>
    public IdShapeTests() => IdShape.Forget();

    /// <summary>
    /// The usual shape: what the stream is, then the value it was named for. The value becomes the
    /// hole and everything the type wrote around it stays put.
    /// </summary>
    [Fact]
    public void Leaves_a_hole_where_the_value_goes()
    {
        IdShape.Of(typeof(SamplePrefixedStreamId))!.Pattern.Should().Be("sample:%");
    }

    [Fact]
    public void Leaves_a_hole_for_every_value()
    {
        IdShape.Of(typeof(SampleTwoPartStreamId))!.Pattern.Should().Be("sample:%:%");
    }

    /// <summary>
    /// A stream whose id is the value it was given and nothing else matches anything at all, which
    /// is the truthful answer: it can name every id in the log.
    /// </summary>
    [Fact]
    public void Is_a_bare_wildcard_when_the_id_is_only_the_value()
    {
        IdShape.Of(typeof(SampleStreamId))!.Pattern.Should().Be("%");
    }

    /// <summary>
    /// Nothing to put in means nothing to leave out: one stream, and the pattern is its id.
    /// </summary>
    [Fact]
    public void Is_the_id_itself_when_the_stream_takes_nothing()
    {
        IdShape.Of(typeof(SampleOnlyStreamId))!.Pattern.Should().Be("samples");
    }

    /// <summary>
    /// A value nothing can stand in for leaves no way to tell which part of the id came from it, so
    /// there is no shape rather than a guessed one.
    /// </summary>
    [Fact]
    public void Has_no_shape_when_a_value_cannot_be_stood_in_for()
    {
        IdShape.Of(typeof(SampleUnprobedStreamId)).Should().BeNull();
    }

    /// <summary>
    /// An aggregate's identifier is read the same way, because it is the same question: it names a
    /// model inside a stream, and the store keeps the id it produced rather than the type that
    /// produced it.
    /// </summary>
    [Fact]
    public void Reads_an_aggregate_identifier_the_same_way()
    {
        IdShape.Of(typeof(SamplePrefixedAggregateId))!.Pattern.Should().Be("order-%");
    }

    /// <summary>
    /// And a projection's, for the same reason.
    /// </summary>
    [Fact]
    public void Reads_a_projection_identifier_the_same_way()
    {
        IdShape.Of(typeof(SamplePrefixedProjectionId))!.Pattern.Should().Be("summary-%");
    }

    /// <summary>
    /// Asked of something that names nothing in a store — an event is written under a key, not
    /// under an id it made up.
    /// </summary>
    [Fact]
    public void Has_no_shape_for_a_type_that_names_nothing()
    {
        IdShape.Of(typeof(SampleCarriedEvent)).Should().BeNull();
    }

    /// <summary>
    /// The pattern is what a stored id is held against, so the shape answers that itself rather than
    /// leaving every caller to work out what a wildcard means.
    /// </summary>
    [Fact]
    public void Matches_an_id_of_its_own_shape()
    {
        var shape = IdShape.Of(typeof(SamplePrefixedAggregateId))!;

        shape.Matches("order-123").Should().BeTrue();
        shape.Matches("order-").Should().BeTrue();
    }

    [Fact]
    public void Does_not_match_an_id_of_another_shape()
    {
        var shape = IdShape.Of(typeof(SamplePrefixedAggregateId))!;

        shape.Matches("summary-123").Should().BeFalse();
        shape.Matches("ORDER-123").Should().BeFalse();
    }

    /// <summary>
    /// A pattern that is only a wildcard matches everything, which is the truthful answer for a type
    /// whose id is the value it was given: it could have produced any of them.
    /// </summary>
    [Fact]
    public void Matches_anything_when_the_pattern_is_a_bare_wildcard()
    {
        IdShape.Of(typeof(SampleStreamId))!.Matches("whatever").Should().BeTrue();
    }

    /// <summary>
    /// A fixed id matches itself and nothing else — there is no hole in it to match anything with.
    /// </summary>
    [Fact]
    public void Matches_only_itself_when_the_pattern_has_no_hole()
    {
        var shape = IdShape.Of(typeof(SampleOnlyStreamId))!;

        shape.Matches("samples").Should().BeTrue();
        shape.Matches("samples-2").Should().BeFalse();
    }

    /// <summary>
    /// The wildcards belong to the pattern, not to what is held against it: a stored id carrying a
    /// percent sign is that character, and matches only a pattern that has one too.
    /// </summary>
    [Fact]
    public void Treats_a_wildcard_in_the_stored_id_as_an_ordinary_character()
    {
        IdShape.Of(typeof(SampleOnlyStreamId))!.Matches("s%mples").Should().BeFalse();
    }

    /// <summary>
    /// Which of a stream's events are one model's is the identifier's to say, and it says it by
    /// naming the properties an event has to carry. Read off the same probe the pattern is: the
    /// keys are the type's, the values beside them are the instance's and say nothing about it.
    /// </summary>
    [Fact]
    public void Reads_the_properties_an_identifier_narrows_its_stream_by()
    {
        IdShape.Of(typeof(SampleFilteredAggregateId))!.Filter.Should().Equal("OrderId");
    }

    /// <summary>
    /// An identifier narrowing by nothing folds the whole stream, which is a different model rather
    /// than a shape that could not be worked out.
    /// </summary>
    [Fact]
    public void Has_no_filter_when_the_identifier_folds_the_whole_stream()
    {
        IdShape.Of(typeof(SamplePrefixedAggregateId))!.Filter.Should().BeEmpty();
    }

    /// <summary>
    /// A stream narrows nothing: it is where events are put, and which of them are a model's is
    /// asked of the identifier folding it.
    /// </summary>
    [Fact]
    public void Has_no_filter_for_a_stream()
    {
        IdShape.Of(typeof(SamplePrefixedStreamId))!.Filter.Should().BeEmpty();
    }

    /// <summary>
    /// The store keeps the id and not the type that made it, so a page holding one stored id and
    /// wanting the thing back has to read the values out of it — which is the pattern run the other
    /// way, its holes read rather than written.
    /// </summary>
    [Fact]
    public void Reads_the_value_an_id_was_built_from()
    {
        IdShape.Of(typeof(SamplePrefixedAggregateId))!.ValuesFrom("order-123")
            .Should().Equal(new Dictionary<string, string> { ["orderId"] = "123" });
    }

    /// <summary>
    /// One value per hole, by the parameter that filled it — which is not always the order the
    /// constructor asks for them in, because a type is free to write them in any order it likes.
    /// </summary>
    [Fact]
    public void Reads_a_value_for_every_hole()
    {
        IdShape.Of(typeof(SampleTwoPartStreamId))!.ValuesFrom("sample:alpha:2024")
            .Should().Equal(new Dictionary<string, string>
            {
                ["sampleId"] = "alpha",
                ["year"] = "2024"
            });
    }

    /// <summary>
    /// An id that is the whole value gives the whole value back: there is nothing around it to
    /// leave out.
    /// </summary>
    [Fact]
    public void Reads_the_whole_id_when_the_pattern_is_a_bare_wildcard()
    {
        IdShape.Of(typeof(SampleStreamId))!.ValuesFrom("whatever")
            .Should().Equal(new Dictionary<string, string> { ["id"] = "whatever" });
    }

    /// <summary>
    /// Nothing went in, so nothing comes out — and the type is still built, from no values at all.
    /// </summary>
    [Fact]
    public void Reads_no_values_when_the_type_takes_none()
    {
        IdShape.Of(typeof(SampleOnlyStreamId))!.ValuesFrom("samples").Should().BeEmpty();
    }

    /// <summary>
    /// An id of another shape has no values of this one in it, so there is nothing to hand back
    /// rather than a guess at which part was which.
    /// </summary>
    [Fact]
    public void Reads_nothing_from_an_id_of_another_shape()
    {
        IdShape.Of(typeof(SamplePrefixedAggregateId))!.ValuesFrom("summary-123").Should().BeNull();
    }

    /// <summary>
    /// Probing means building one, so the answer is kept: neither the type nor what it produces
    /// changes while the assemblies are loaded.
    /// </summary>
    [Fact]
    public void Is_the_same_shape_when_asked_again()
    {
        IdShape.Of(typeof(SamplePrefixedStreamId))
            .Should().BeSameAs(IdShape.Of(typeof(SamplePrefixedStreamId)));
    }
}
