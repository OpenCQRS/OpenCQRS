using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Reading the two sequences the compare tab folds up to out of the address. What is pinned is
/// which pairs are a comparison and which are refused with a reason, and what the tab compares
/// when the address names nothing: the last event against the state before it.
/// </summary>
public class CompareRangeTests
{
    [Fact]
    public void Reads_a_pair_of_sequences()
    {
        var read = CompareRange.Of("3", "7", lastSequence: 14);

        read.Range.Should().Be(new CompareRange(3, 7));
        read.Error.Should().BeNull();
    }

    /// <summary>
    /// The state before anything happened is a fold up to zero, so zero is a sequence to compare
    /// from — and the last event of the stream is one to compare up to.
    /// </summary>
    [Fact]
    public void Allows_the_fold_before_the_first_event_and_the_fold_up_to_the_last()
    {
        var read = CompareRange.Of("0", "14", lastSequence: 14);

        read.Range.Should().Be(new CompareRange(0, 14));
    }

    [Fact]
    public void Compares_the_last_event_against_the_state_before_it_when_nothing_is_asked_for()
    {
        var read = CompareRange.Of(null, null, lastSequence: 14);

        read.Range.Should().Be(new CompareRange(13, 14));
        read.Error.Should().BeNull();
    }

    [Fact]
    public void Has_nothing_to_compare_when_nothing_is_asked_for_and_the_stream_is_empty()
    {
        var read = CompareRange.Of(null, null, lastSequence: 0);

        read.Range.Should().BeNull();
        read.Error.Should().Contain("no events");
    }

    /// <summary>
    /// When the history could not be counted, there is no last event to default to and no bound to
    /// check against — so a pair given is taken as it is, and nothing is guessed for a pair not.
    /// </summary>
    [Fact]
    public void Takes_a_pair_as_given_when_the_last_sequence_is_unknown()
    {
        CompareRange.Of("3", "7", lastSequence: null).Range.Should().Be(new CompareRange(3, 7));
        CompareRange.Of(null, null, lastSequence: null).Range.Should().BeNull();
        CompareRange.Of(null, null, lastSequence: null).Error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("3", null)]
    [InlineData(null, "7")]
    [InlineData("", "7")]
    public void Refuses_half_a_pair(string? from, string? to)
    {
        var read = CompareRange.Of(from, to, lastSequence: 14);

        read.Range.Should().BeNull();
        read.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("three", "7")]
    [InlineData("3", "7.5")]
    [InlineData("3", "99999999999")]
    public void Refuses_a_value_that_is_not_a_whole_number_the_store_can_fold_to(string from, string to)
    {
        var read = CompareRange.Of(from, to, lastSequence: 14);

        read.Range.Should().BeNull();
        read.Error.Should().Contain("whole number");
    }

    [Fact]
    public void Refuses_a_negative_sequence()
    {
        var read = CompareRange.Of("-1", "7", lastSequence: 14);

        read.Range.Should().BeNull();
        read.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("7", "7")]
    [InlineData("8", "7")]
    public void Refuses_a_pair_that_does_not_run_forward(string from, string to)
    {
        var read = CompareRange.Of(from, to, lastSequence: 14);

        read.Range.Should().BeNull();
        read.Error.Should().Contain("below");
    }

    [Fact]
    public void Refuses_a_sequence_past_the_last_event()
    {
        var read = CompareRange.Of("3", "15", lastSequence: 14);

        read.Range.Should().BeNull();
        read.Error.Should().Contain("14");
    }

    [Fact]
    public void Allows_a_sequence_at_the_last_event()
    {
        CompareRange.Of("3", "14", lastSequence: 14).Range.Should().Be(new CompareRange(3, 14));
    }
}
