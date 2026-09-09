using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which streams the streamed events data page was asked for: every one of them, the ones of a
/// single stream type, or neither because the name in the address reaches nothing registered.
/// Reading the rows themselves is the database's work — see <see cref="StreamedEvents.Page"/> — and
/// is covered against a real store rather than here.
/// </summary>
public class StreamChoiceTests
{
    /// <summary>
    /// The stream types the uploaded assemblies declare, which is what a name arriving in the
    /// address is matched against. Types rather than the ids in the log: one type names as many
    /// streams as it has been given values, and a list of those would be as long as the log is
    /// wide.
    /// </summary>
    private static readonly IReadOnlyList<Type> Streams =
        [typeof(SamplePrefixedStreamId), typeof(SampleOnlyStreamId), typeof(SampleUnprobedStreamId)];

    /// <summary>
    /// The table opens on the whole log. Nothing has to be chosen before there is something to read.
    /// </summary>
    [Fact]
    public void Shows_every_stream_when_none_was_asked_for()
    {
        var choice = StreamChoice.Of(Streams, asked: null);

        choice.IsAll.Should().BeTrue();
        choice.Stream.Should().BeNull();
        choice.Pattern.Should().BeNull();
    }

    /// <summary>
    /// Which is what the dropdown submits when its first item is chosen.
    /// </summary>
    [Fact]
    public void Shows_every_stream_when_the_name_asked_for_is_empty()
    {
        StreamChoice.Of(Streams, asked: "").IsAll.Should().BeTrue();
    }

    [Fact]
    public void Narrows_to_the_stream_type_asked_for()
    {
        var choice = StreamChoice.Of(Streams, typeof(SamplePrefixedStreamId).FullName);

        choice.Stream.Should().Be(typeof(SamplePrefixedStreamId));
        choice.IsAll.Should().BeFalse();
        choice.ShowsNothing.Should().BeFalse();
    }

    /// <summary>
    /// The log stores an id rather than a type, so narrowing to a type means narrowing to the
    /// pattern the ids it makes all match.
    /// </summary>
    [Fact]
    public void Narrows_by_the_pattern_the_ids_of_that_type_match()
    {
        StreamChoice.Of(Streams, typeof(SamplePrefixedStreamId).FullName)
            .Pattern.Should().Be("sample:%");
    }

    /// <summary>
    /// Asking for a type that is not registered is not the same as asking for all of them.
    /// Answering it with the whole log would look like the filter had been applied, and read as
    /// that stream holding events appended somewhere else.
    /// </summary>
    [Fact]
    public void Reports_a_type_that_is_not_registered_as_unknown()
    {
        var choice = StreamChoice.Of(Streams, "Nothing.Uploaded.Declares.This");

        choice.Unknown.Should().BeTrue();
        choice.IsAll.Should().BeFalse();
        choice.Stream.Should().BeNull();
        choice.ShowsNothing.Should().BeTrue();
    }

    /// <summary>
    /// Registered, listed, and impossible to narrow by: a stream built from a value nothing can
    /// stand in for has no pattern, so there is nothing to ask the log for. Said apart from an
    /// unknown name because it is a different answer — the type is there, and what cannot be done
    /// is the matching.
    /// </summary>
    [Fact]
    public void Reports_a_stream_whose_pattern_cannot_be_worked_out()
    {
        var choice = StreamChoice.Of(Streams, typeof(SampleUnprobedStreamId).FullName);

        choice.Unshaped.Should().BeTrue();
        choice.Unknown.Should().BeFalse();
        choice.Pattern.Should().BeNull();
        choice.ShowsNothing.Should().BeTrue();
    }

    /// <summary>
    /// What the count is counted in, so a narrowed table says what it was narrowed to — the type,
    /// as the streams page names it, rather than the pattern it happens to match by.
    /// </summary>
    [Fact]
    public void Is_named_by_the_stream_type_when_one_was_chosen()
    {
        StreamChoice.Of(Streams, typeof(SampleOnlyStreamId).FullName)
            .Name.Should().Be("SampleOnlyStreamId");
    }

    [Fact]
    public void Is_named_as_every_stream_when_none_was()
    {
        StreamChoice.Of(Streams, asked: null).Name.Should().Be("All streams");
    }
}
