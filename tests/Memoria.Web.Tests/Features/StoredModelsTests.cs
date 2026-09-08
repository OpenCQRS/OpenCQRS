using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Reading one stored row back into the types that made it: the model its key names, and the
/// identifier that addresses the boundary it was folded under. What a list of every model's rows
/// needs in order to name a row and link it to the page about it.
/// </summary>
public class StoredModelsTests
{
    private static readonly IReadOnlyList<Type> Models =
        [typeof(SampleDcbAggregate), typeof(SampleCarryingDcbAggregate)];

    private static readonly IReadOnlyList<Type> Identifiers =
        [typeof(SampleDcbAggregateId), typeof(SampleTwoPartId), typeof(SampleAllOfId)];

    private static readonly StoredModels Stored = new(Models, Identifiers);

    [Fact]
    public void Reads_the_model_the_stored_key_names()
    {
        Stored.Of("SampleDcbAggregate:1", "sample:abc").Model.Should().Be(typeof(SampleDcbAggregate));
    }

    /// <summary>
    /// A row whose type was never uploaded, or was uploaded and then replaced. It is still worth
    /// listing — the boundary, the version and the dates are facts of the store itself — but there
    /// is no page about it to lead to.
    /// </summary>
    [Fact]
    public void Lists_a_row_whose_type_is_not_among_the_uploaded_ones()
    {
        var stored = Stored.Of("NeverUploaded:1", "sample:abc");

        stored.Model.Should().BeNull();
        stored.Identifier.Should().BeNull();
        stored.IsAddressable.Should().BeFalse();
    }

    [Fact]
    public void Reads_the_identifier_that_addresses_the_stored_boundary()
    {
        var stored = Stored.Of("SampleDcbAggregate:1", "sample:abc");

        stored.Identifier.Should().Be(typeof(SampleDcbAggregateId));
        stored.Values.Should().Contain(new KeyValuePair<string, string>("id", "abc"));
        stored.IsAddressable.Should().BeTrue();
    }

    /// <summary>
    /// The identifier is confirmed by building it and rendering it back, so a boundary is only
    /// claimed by the identifier that actually produces it. A narrower identifier carries tags a
    /// wider boundary also has — <c>sample:abc</c> is inside <c>label:x,sample:abc</c> — and reading
    /// the tags alone would let it claim rows it does not address. It is tried first here, so a
    /// pass would be one that took it.
    /// </summary>
    [Fact]
    public void Does_not_let_a_narrower_identifier_claim_a_wider_boundary()
    {
        var stored = Stored.Of("SampleDcbAggregate:1", "label:x,sample:abc");

        stored.Identifier.Should().Be(typeof(SampleTwoPartId));
        stored.Values.Should().Contain(new KeyValuePair<string, string>("id", "abc"))
            .And.Contain(new KeyValuePair<string, string>("label", "x"));
    }

    /// <summary>
    /// Two identifiers can carry the very same tags and differ only in how they combine them — a
    /// union selects events inside either tag, an intersection only those inside both — so the
    /// rendering is what tells them apart, and a row is addressed by the one that renders to it.
    /// The store writes a union's tags as separate groups and an intersection's as one, which is
    /// the whole difference between these two boundaries.
    /// </summary>
    [Fact]
    public void Tells_identifiers_apart_by_how_their_tags_combine()
    {
        var union = Stored.Of("SampleDcbAggregate:1", "label:x,sample:abc");
        var intersection = Stored.Of("SampleDcbAggregate:1", "label:x&sample:abc");

        union.Identifier.Should().Be(typeof(SampleTwoPartId));
        intersection.Identifier.Should().Be(typeof(SampleAllOfId));
    }

    /// <summary>
    /// A boundary no identifier of the model renders. The row is still named and listed; there is
    /// just no way in to a page about it.
    /// </summary>
    [Fact]
    public void Names_a_row_no_identifier_addresses()
    {
        var stored = Stored.Of("SampleDcbAggregate:1", "unrelated:abc");

        stored.Model.Should().Be(typeof(SampleDcbAggregate));
        stored.Identifier.Should().BeNull();
        stored.Values.Should().BeEmpty();
        stored.IsAddressable.Should().BeFalse();
    }

    /// <summary>
    /// An identifier of another model never addresses this one's rows, whatever tags they share.
    /// </summary>
    [Fact]
    public void Does_not_address_a_row_with_another_models_identifier()
    {
        var stored = Stored.Of("SampleCarryingAggregate:1", "sample:abc");

        stored.Model.Should().Be(typeof(SampleCarryingDcbAggregate));
        stored.Identifier.Should().BeNull();
    }

    /// <summary>
    /// Resolving means building identifiers and rendering their boundaries, and a page of rows asks
    /// about the same few types over and over — so what is worked out for one row answers the next.
    /// </summary>
    [Fact]
    public void Works_a_models_identifiers_out_once_for_the_whole_page()
    {
        var first = Stored.Of("SampleDcbAggregate:1", "sample:abc");
        var second = Stored.Of("SampleDcbAggregate:1", "sample:def");

        first.Model.Should().BeSameAs(second.Model);
        first.Identifier.Should().BeSameAs(second.Identifier);
        second.Values.Should().Contain(new KeyValuePair<string, string>("id", "def"));
    }
}
