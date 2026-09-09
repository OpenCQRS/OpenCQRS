using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which registered type a data page was narrowed to: all of them, one of them, or neither because
/// the name in the address reaches nothing that could have been written. Reading the rows themselves
/// is the database's work — see <see cref="AppendedEvents.Page"/> and its streamed counterparts —
/// and is covered against a real store rather than here.
/// </summary>
/// <remarks>
/// Exercised through events, which is one of the three kinds of type it serves. What it reads is the
/// binding key, off whichever attribute the type carries, so an aggregate and a projection go the
/// same way through it.
/// </remarks>
public class TypeChoiceTests
{
    /// <summary>
    /// What the events page lists, which is what a name arriving in the address is matched against.
    /// </summary>
    private static readonly IReadOnlyList<Type> Events =
        [typeof(SampleHappenedEvent), typeof(SampleCarriedEvent), typeof(SampleUnboundEvent)];

    /// <summary>
    /// The table opens on the whole log. Nothing has to be chosen before there is something to read.
    /// </summary>
    [Fact]
    public void Shows_every_type_when_none_was_asked_for()
    {
        var choice = TypeChoice.Of(Events, asked: null);

        choice.IsAll.Should().BeTrue();
        choice.Chosen.Should().BeNull();
        choice.Key.Should().BeNull();
    }

    /// <summary>
    /// Which is what the dropdown submits when its first item is chosen.
    /// </summary>
    [Fact]
    public void Shows_every_type_when_the_name_asked_for_is_empty()
    {
        TypeChoice.Of(Events, asked: "").IsAll.Should().BeTrue();
    }

    [Fact]
    public void Narrows_to_the_type_asked_for()
    {
        var choice = TypeChoice.Of(Events, typeof(SampleCarriedEvent).FullName);

        choice.Chosen.Should().Be(typeof(SampleCarriedEvent));
        choice.IsAll.Should().BeFalse();
        choice.ShowsNothing.Should().BeFalse();
    }

    /// <summary>
    /// The log stores a key rather than a class, so narrowing to a type means narrowing to the key
    /// that type is written under.
    /// </summary>
    [Fact]
    public void Narrows_by_the_key_the_log_writes_the_type_under()
    {
        TypeChoice.Of(Events, typeof(SampleHappenedEvent).FullName).Key.Should().Be("SampleHappened:1");
    }

    /// <summary>
    /// A name left over from an earlier upload. Answering it with the whole log would look like the
    /// filter had been applied and read as the log holding events it does not, so it is told apart
    /// from asking for none.
    /// </summary>
    [Fact]
    public void Tells_a_name_it_does_not_know_apart_from_asking_for_none()
    {
        var choice = TypeChoice.Of(Events, "Never.Uploaded.Event");

        choice.Unknown.Should().BeTrue();
        choice.IsAll.Should().BeFalse();
        choice.Chosen.Should().BeNull();
        choice.ShowsNothing.Should().BeTrue();
    }

    /// <summary>
    /// An event carrying no [EventType] is written into the log by nothing, so there is no key to
    /// narrow by — and narrowing by nothing would show every event under its name.
    /// </summary>
    [Fact]
    public void Says_a_type_the_log_could_never_have_written()
    {
        var choice = TypeChoice.Of(Events, typeof(SampleUnboundEvent).FullName);

        choice.Chosen.Should().Be(typeof(SampleUnboundEvent));
        choice.Unbound.Should().BeTrue();
        choice.Key.Should().BeNull();
        choice.ShowsNothing.Should().BeTrue();
    }

    [Fact]
    public void A_type_the_log_writes_is_not_one_it_could_never_have_written()
    {
        TypeChoice.Of(Events, typeof(SampleHappenedEvent).FullName).Unbound.Should().BeFalse();
    }

    /// <summary>
    /// The line counting what a page holds names what was narrowed to. A chosen type is named as the
    /// store names it, and what is being read when nothing was chosen is everything.
    /// </summary>
    [Fact]
    public void Names_what_is_being_read()
    {
        TypeChoice.Of(Events, asked: null).Name.Should().Be("All types");
        TypeChoice.Of(Events, typeof(SampleHappenedEvent).FullName).Name.Should().Be("SampleHappened");
        TypeChoice.Of(Events, typeof(SampleUnboundEvent).FullName).Name.Should().Be("SampleUnboundEvent");
    }

    /// <summary>
    /// A name reaching nothing names nothing, so it falls back to the plainest thing there is to say
    /// rather than claiming everything stored is being shown under it.
    /// </summary>
    [Fact]
    public void Names_nothing_in_particular_when_the_name_reaches_nothing()
    {
        TypeChoice.Of(Events, "Never.Uploaded.Event").Name.Should().Be("Types");
    }
}
