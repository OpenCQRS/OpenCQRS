using System;
using System.IO;
using FluentAssertions;
using Memoria.EventSourcing.Store.Cosmos;
using Memoria.Web.Data;
using Memoria.Web.Samples.Seeding;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Memoria.Web.Samples.Tests.Features;

/// <summary>
/// Which data a run may be asked for, which is not the same question in every store.
/// </summary>
/// <remarks>
/// A relational store holds both models, so there is something to choose between. A Cosmos store
/// holds only the streamed one — there is no dynamic consistency boundary store for Cosmos at all —
/// so offering the choice would be offering something that cannot be done. The store says what it
/// holds and the question is narrowed to that.
/// <para>
/// The answer is read from standard input, so these redirect it. End of input means nobody answered,
/// which the menu reports as <see cref="SampleDataScope.None"/> — that is what makes a question
/// asked distinguishable here from one that was not.
/// </para>
/// </remarks>
public class SampleDataScopeTests : IDisposable
{
    private readonly TextReader _input = Console.In;
    private readonly TextWriter _output = Console.Out;

    public SampleDataScopeTests() => Console.SetOut(new StringWriter());

    public void Dispose()
    {
        Console.SetIn(_input);
        Console.SetOut(_output);
    }

    /// <summary>
    /// Nothing is asked when only one kind can be written. Proven by leaving standard input empty:
    /// a question asked would find no answer there and come back as nothing chosen.
    /// </summary>
    [Fact]
    public void Does_not_ask_which_data_when_the_store_holds_only_one_kind()
    {
        Console.SetIn(new StringReader(string.Empty));

        Menu.AskForScope(SampleDataScope.Streamed).Should().Be(SampleDataScope.Streamed);
    }

    [Theory]
    [InlineData("1", SampleDataScope.Streamed)]
    [InlineData("2", SampleDataScope.Dcb)]
    [InlineData("3", SampleDataScope.Both)]
    public void Offers_every_kind_and_both_when_the_store_holds_both(string answer, SampleDataScope expected)
    {
        Console.SetIn(new StringReader(answer));

        Menu.AskForScope(SampleDataScope.Both).Should().Be(expected);
    }

    [Fact]
    public void Reports_nothing_chosen_when_a_question_it_asked_goes_unanswered()
    {
        Console.SetIn(new StringReader(string.Empty));

        Menu.AskForScope(SampleDataScope.Both).Should().Be(SampleDataScope.None);
    }

    /// <summary>
    /// Where the narrowing comes from: the store itself, rather than a provider comparison written
    /// wherever the question is asked.
    /// </summary>
    [Fact]
    public void A_cosmos_store_holds_only_streamed_sample_data()
    {
        var store = new CosmosStore(
            "AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", "Memoria", "Domain");

        using var client = new CosmosClient(store.ConnectionString);
        using var provider = new CosmosClientProvider(client, store.DatabaseName, store.ContainerName);

        new CosmosSampleStore(provider, store).Holds.Should().Be(SampleDataScope.Streamed);
    }
}
