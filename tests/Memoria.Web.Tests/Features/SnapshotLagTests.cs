using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Whether a stored snapshot has been left behind by the history it was folded from, which is what
/// the version row on a detail page warns about.
/// </summary>
public class SnapshotLagTests
{
    /// <summary>
    /// The ordinary case: the row was written by folding everything there was, and there is nothing
    /// to warn about.
    /// </summary>
    [Fact]
    public void Says_nothing_when_the_version_has_folded_every_event()
    {
        SnapshotLag.Of(version: 4, events: 4).Should().BeNull();
    }

    /// <summary>
    /// A version above the count is not a lag, and saying it was behind by a negative number of
    /// events would be worse than saying nothing. A model that declines events inside its own
    /// filter reads this way round, so it is an ordinary answer rather than an impossible one.
    /// </summary>
    [Fact]
    public void Says_nothing_when_the_version_is_past_the_events_counted()
    {
        SnapshotLag.Of(version: 6, events: 4).Should().BeNull();
    }

    /// <summary>
    /// Nothing counted is not nothing there. The count is a read of its own and it can fail, and a
    /// page that cannot count the history has no grounds to say the snapshot is behind it.
    /// </summary>
    [Fact]
    public void Says_nothing_when_the_events_could_not_be_counted()
    {
        SnapshotLag.Of(version: 4, events: null).Should().BeNull();
    }

    [Fact]
    public void Counts_how_far_behind_the_version_is()
    {
        var lag = SnapshotLag.Of(version: 3, events: 7);

        lag.Should().NotBeNull();
        lag!.Version.Should().Be(3);
        lag.Events.Should().Be(7);
        lag.Behind.Should().Be(4);
    }

    [Fact]
    public void Says_how_many_events_were_never_folded_in()
    {
        SnapshotLag.Of(version: 3, events: 7)!.Text
            .Should().Be("Behind its history by 4 events. Updating this snapshot would fold them in.");
    }

    /// <summary>
    /// One event is counted in words rather than as a digit, and the sentence agrees with it. The
    /// warning is read as prose beside a number, and "1 events" beside it would read as a bug.
    /// </summary>
    [Fact]
    public void Names_a_single_missing_event_in_the_singular()
    {
        SnapshotLag.Of(version: 3, events: 4)!.Text
            .Should().Be("Behind its history by one event. Updating this snapshot would fold it in.");
    }
}
