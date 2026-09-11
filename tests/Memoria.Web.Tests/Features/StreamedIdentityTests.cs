using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Working one stored row back to the two things that made it. The store keeps the ids a stream and
/// an identifier produced rather than the types that produced them, so a page holding a row has to
/// recover both: the events tab narrows by what the identifier claims, and the update tab hands the
/// pair to the store, which folds a stream through an identifier and takes the things rather than
/// the strings.
/// </summary>
public class StreamedIdentityTests
{
    /// <summary>
    /// Worked out once and kept, so a test that has already asked is not what the next one reads.
    /// </summary>
    public StreamedIdentityTests() => IdShape.Forget();

    private static readonly DomainTypeCatalogue Catalogue = new()
    {
        StreamedStreamIds = [typeof(SamplePrefixedStreamId)],
        StreamedAggregates = [typeof(SampleAggregate)],
        StreamedAggregateIds = [typeof(SamplePrefixedAggregateId), typeof(SampleFilteredAggregateId)],
        StreamedProjections = [typeof(SampleProjection)],
        StreamedProjectionIds = [typeof(SamplePrefixedProjectionId)]
    };

    [Fact]
    public void Names_and_rebuilds_the_stream_the_row_was_folded_from()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "order-123");

        identity.StreamType.Should().Be<SamplePrefixedStreamId>();
        identity.Stream.Should().BeOfType<SamplePrefixedStreamId>()
            .Which.Id.Should().Be("sample:alpha");
    }

    [Fact]
    public void Names_and_rebuilds_the_identifier_that_addressed_the_row()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "order-123");

        identity.IdentifierType.Should().Be<SamplePrefixedAggregateId>();
        identity.Identifier.Should().BeOfType<SamplePrefixedAggregateId>()
            .Which.Id.Should().Be("order-123");
    }

    /// <summary>
    /// A read model is addressed by its own kind of identifier, so the aggregate's list is not the
    /// one a projection's row is matched against.
    /// </summary>
    [Fact]
    public void Takes_the_identifier_from_the_list_of_the_kind_being_read()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Projection, typeof(SampleProjection), "sample:alpha", "summary-7");

        identity.IdentifierType.Should().Be<SamplePrefixedProjectionId>();
        identity.Identifier.Should().BeOfType<SamplePrefixedProjectionId>();
    }

    /// <summary>
    /// What the identifier claims of its stream, read off the rebuilt instance rather than off its
    /// type: the shape knows which properties are claimed and only one built from this row's own
    /// values knows what they have to equal.
    /// </summary>
    [Fact]
    public void Reads_what_the_rebuilt_identifier_claims_of_its_stream()
    {
        var narrowing = new DomainTypeCatalogue
        {
            StreamedStreamIds = Catalogue.StreamedStreamIds,
            StreamedAggregates = Catalogue.StreamedAggregates,
            StreamedAggregateIds = [typeof(SampleFilteredAggregateId)]
        };

        var identity = StreamedIdentity.Of(
            narrowing, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "order-123");

        identity.Claim.Should().Equal(new Dictionary<string, string> { ["OrderId"] = "123" });
    }

    /// <summary>
    /// An identifier narrowing by nothing folds the whole stream, which is an empty claim rather
    /// than an unknown one.
    /// </summary>
    [Fact]
    public void Claims_nothing_when_the_identifier_folds_the_whole_stream()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "order-123");

        identity.Claim.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>
    /// Nothing registered writes ids of this shape. Null and empty are different answers: empty is a
    /// model folding the whole stream, and this is not knowing — which is a wider history than one
    /// model's, and a row nothing can be written for.
    /// </summary>
    [Fact]
    public void Recovers_no_identifier_from_an_id_nothing_registered_writes()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "nothing-like-this");

        identity.IdentifierType.Should().BeNull();
        identity.Identifier.Should().BeNull();
        identity.Claim.Should().BeNull();
        identity.IsRecovered.Should().BeFalse();
    }

    [Fact]
    public void Recovers_no_stream_from_an_id_nothing_registered_writes()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "other:alpha", "order-123");

        identity.StreamType.Should().BeNull();
        identity.Stream.Should().BeNull();
        identity.IsRecovered.Should().BeFalse();
    }

    /// <summary>
    /// Both halves, which is what the store needs to be asked to write: it folds a stream through an
    /// identifier, and one without the other addresses nothing.
    /// </summary>
    [Fact]
    public void Is_recovered_when_both_halves_were_built()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", "order-123");

        identity.IsRecovered.Should().BeTrue();
    }

    /// <summary>
    /// A row whose key carries no id of its own leaves the identifier unknown, and the stream is
    /// still worked out: the events in it are worth seeing either way.
    /// </summary>
    [Fact]
    public void Recovers_the_stream_alone_when_there_is_no_id_to_match()
    {
        var identity = StreamedIdentity.Of(
            Catalogue, StreamedModelKind.Aggregate, typeof(SampleAggregate), "sample:alpha", addressedId: null);

        identity.Stream.Should().NotBeNull();
        identity.Identifier.Should().BeNull();
        identity.IsRecovered.Should().BeFalse();
    }
}
