using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which stored models a data page was asked for: every one of its kind, one type of them, or one
/// type addressed one way. Reading the rows themselves is the database's work — see
/// <see cref="IdentifierInstances.Page"/> — and is covered against a real store rather than here.
/// </summary>
public class ModelChoiceTests
{
    /// <summary>
    /// What the aggregates page lists, which is what the two names arriving in the address are
    /// matched against.
    /// </summary>
    private static readonly IReadOnlyList<Type> Models =
        [typeof(SampleDcbAggregate), typeof(SampleCarryingDcbAggregate)];

    private static readonly IReadOnlyList<Type> Identifiers =
        [typeof(SampleDcbAggregateId), typeof(SampleTwoPartId), typeof(SampleUnmappedId)];

    private static ModelChoice Choose(string? model, string? identifier = null) =>
        ModelChoice.Of(Models, Identifiers, model, identifier);

    /// <summary>
    /// The table opens on everything stored of its kind. Nothing has to be chosen before there is
    /// something to read.
    /// </summary>
    [Fact]
    public void Shows_every_model_when_none_was_asked_for()
    {
        var choice = Choose(model: null);

        choice.IsAllModels.Should().BeTrue();
        choice.IsAllIdentifiers.Should().BeTrue();
        choice.Model.Should().BeNull();
        choice.Key.Should().BeNull();
        choice.Shape.Should().BeNull();
    }

    /// <summary>
    /// Which is what the dropdown submits when its first item is chosen.
    /// </summary>
    [Fact]
    public void Shows_every_model_when_the_name_asked_for_is_empty()
    {
        Choose(model: "").IsAllModels.Should().BeTrue();
    }

    [Fact]
    public void Narrows_to_the_model_asked_for()
    {
        var choice = Choose(typeof(SampleDcbAggregate).FullName);

        choice.Model.Should().Be(typeof(SampleDcbAggregate));
        choice.IsAllModels.Should().BeFalse();
        choice.ShowsNothing.Should().BeFalse();
    }

    /// <summary>
    /// The store keeps a key rather than a class, so narrowing to a type means narrowing to the key
    /// its snapshots were written under.
    /// </summary>
    [Fact]
    public void Narrows_by_the_key_the_store_writes_the_model_under()
    {
        Choose(typeof(SampleDcbAggregate).FullName).Key.Should().Be("SampleDcbAggregate:1");
    }

    /// <summary>
    /// A name left over from an earlier upload. Answering it with everything stored would look like
    /// the filter had been applied, so it is told apart from asking for none.
    /// </summary>
    [Fact]
    public void Tells_a_model_it_does_not_know_apart_from_asking_for_none()
    {
        var choice = Choose("Never.Uploaded.Aggregate");

        choice.Unknown.Should().BeTrue();
        choice.IsAllModels.Should().BeFalse();
        choice.ShowsNothing.Should().BeTrue();
    }

    /// <summary>
    /// A model carrying no [AggregateType] is written into the store by nothing, so there is no key
    /// to narrow by — and narrowing by nothing would show every model's rows under its name.
    /// </summary>
    [Fact]
    public void Says_a_model_the_store_could_never_have_written()
    {
        var choice = ModelChoice.Of([typeof(SampleUnboundDcbAggregate)], Identifiers,
            typeof(SampleUnboundDcbAggregate).FullName, identifier: null);

        choice.Unbound.Should().BeTrue();
        choice.Key.Should().BeNull();
        choice.ShowsNothing.Should().BeTrue();
    }

    /// <summary>
    /// The identifiers on offer are the chosen model's own, which is what the second dropdown lists.
    /// </summary>
    [Fact]
    public void Offers_the_identifiers_that_address_the_chosen_model()
    {
        Choose(typeof(SampleDcbAggregate).FullName).Identifiers
            .Should().Contain(typeof(SampleDcbAggregateId))
            .And.Contain(typeof(SampleTwoPartId));
    }

    /// <summary>
    /// There is nothing to offer until a model is chosen: an identifier addresses one model, so a
    /// list of them across all models would be a list of choices that mostly do not apply.
    /// </summary>
    [Fact]
    public void Offers_no_identifier_until_a_model_is_chosen()
    {
        Choose(model: null).Identifiers.Should().BeEmpty();
    }

    [Fact]
    public void Narrows_to_the_identifier_asked_for()
    {
        var choice = Choose(typeof(SampleDcbAggregate).FullName, typeof(SampleTwoPartId).FullName);

        choice.Identifier.Should().Be(typeof(SampleTwoPartId));
        choice.IsAllIdentifiers.Should().BeFalse();
        choice.Shape.Should().NotBeNull();
        choice.ShowsNothing.Should().BeFalse();
    }

    /// <summary>
    /// The two dropdowns are one form, so changing the model submits the identifier that was chosen
    /// for the model before it. That is the ordinary way to arrive here, not a mistake worth an
    /// alert — the identifier simply does not apply, and every one of the new model is shown.
    /// </summary>
    [Fact]
    public void Widens_to_every_identifier_when_the_one_asked_for_addresses_another_model()
    {
        var choice = Choose(typeof(SampleCarryingDcbAggregate).FullName,
            typeof(SampleDcbAggregateId).FullName);

        choice.Model.Should().Be(typeof(SampleCarryingDcbAggregate));
        choice.Identifier.Should().BeNull();
        choice.IsAllIdentifiers.Should().BeTrue();
        choice.ShowsNothing.Should().BeFalse();
    }

    [Fact]
    public void Widens_to_every_identifier_when_the_one_asked_for_is_not_known()
    {
        Choose(typeof(SampleDcbAggregate).FullName, "Never.Uploaded.Id")
            .IsAllIdentifiers.Should().BeTrue();
    }

    /// <summary>
    /// An identifier names one model's way in, so without a model there is nothing for it to be a
    /// way into.
    /// </summary>
    [Fact]
    public void Ignores_an_identifier_asked_for_without_a_model()
    {
        var choice = Choose(model: null, typeof(SampleDcbAggregateId).FullName);

        choice.IsAllModels.Should().BeTrue();
        choice.IsAllIdentifiers.Should().BeTrue();
    }

    /// <summary>
    /// A deliberate choice from the dropdown that still cannot draw a table: the identifier takes a
    /// value its boundary never mentions, so which of these exist cannot be worked out from the
    /// stored tags. Said rather than quietly widened, because the reader asked for this one.
    /// </summary>
    [Fact]
    public void Says_an_identifier_whose_values_cannot_be_read_back()
    {
        var choice = Choose(typeof(SampleDcbAggregate).FullName, typeof(SampleUnmappedId).FullName);

        choice.Identifier.Should().Be(typeof(SampleUnmappedId));
        choice.Shapeless.Should().BeTrue();
        choice.Shape.Should().BeNull();
        choice.IsAllIdentifiers.Should().BeFalse();
        choice.ShowsNothing.Should().BeTrue();
    }
}
