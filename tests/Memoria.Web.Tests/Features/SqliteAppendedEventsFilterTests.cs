using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Memoria.Web.Data;
using Memoria.Web.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// What the text box on the DCB event data page reaches.
/// </summary>
/// <remarks>
/// One box over the two things a row is written with: its payload, as it was serialised, and the
/// tags it was appended under. Both, because a DCB event belongs to no stream — a tag is the only
/// way a boundary reaches it, so a reader who knows the tag and not the payload would otherwise
/// have no way in. Not the position: a box that also matched it answered a search for a reference
/// beginning <c>14</c> with the fourteenth row as well, and a reader has no way to tell the extra
/// row from a real match.
/// <para>
/// A SQLite file rather than an in-memory provider, because the narrowing is the database's: a
/// predicate that reads one way in LINQ-to-Objects and another in SQL is exactly what this is for.
/// </para>
/// </remarks>
public class SqliteAppendedEventsFilterTests : IAsyncLifetime
{
    private readonly string _file =
        Path.Combine(Path.GetTempPath(), $"memoria_web_dcb_filter_{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_file}";

    private static DbContextOptions<DcbDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<DcbDbContext>().UseSqlite(connectionString).Options;

    private DcbStoreDbContext Store() =>
        new(Options(ConnectionString), TimeProvider.System, Substitute.For<IHttpContextAccessor>());

    public async Task InitializeAsync()
    {
        await using var seed = Store();

        await seed.Database.EnsureCreatedAsync();

        // Positions are the database's, so these take one, two and three in order. Neither a
        // payload nor a tag carries a digit, which is what leaves a numeric search with nothing but
        // the position to match on. The tag is a word no payload holds, so a row matched through it
        // was matched through it and not through the text beside it.
        foreach (var (reference, region) in new[]
                 {
                     ("alpha", "west"), ("beta", "east"), ("gamma", "west")
                 })
        {
            seed.DcbEvents.Add(new DcbEventEntity
            {
                EventType = "OrderPlacedEvent:1",
                Data = $"{{\"reference\":\"{reference}\"}}",
                Tags = [new DcbEventTagEntity { Tag = $"region:{region}" }]
            });

            await seed.SaveChangesAsync();
        }
    }

    public Task DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            File.Delete(_file);
        }
        catch (IOException)
        {
            // A file the operating system is still holding is not this test's problem.
        }

        return Task.CompletedTask;
    }

    private Task<StoredEvents> Filtered(string text) =>
        AppendedEvents.Page(Store(), eventType: null, text, descending: true, page: 1, size: 10);

    [Fact]
    public async Task GivenTextInAPayload_WhenTheLogIsFiltered_ThenTheRowCarryingItComesBack()
    {
        var page = await Filtered("beta");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(1);
    }

    /// <summary>
    /// A number is text like any other here: it narrows to the payloads carrying it, and the second
    /// row is not one of them merely because it is the second.
    /// </summary>
    [Fact]
    public async Task GivenANumber_WhenTheLogIsFiltered_ThenNoRowIsMatchedByItsPosition()
    {
        var page = await Filtered("2");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(0, "the box narrows by payload and tag, and neither carries a 2");
    }

    /// <summary>
    /// A tag is how a DCB event is reached, so the box that narrows the log reaches it too.
    /// </summary>
    [Fact]
    public async Task GivenTextInATag_WhenTheLogIsFiltered_ThenTheRowsAppendedUnderItComeBack()
    {
        var page = await Filtered("region:west");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(2, "two rows were appended under that tag, and no payload holds it");
    }

    /// <summary>
    /// Part of a tag, matched the way part of a payload is: a reader narrowing the log knows a value
    /// far more often than the key it was written under.
    /// </summary>
    [Fact]
    public async Task GivenPartOfATag_WhenTheLogIsFiltered_ThenTheRowAppendedUnderItComesBack()
    {
        var page = await Filtered("east");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(1);
    }

    /// <summary>
    /// One box over two things is one answer: the rows carrying the text in either, counted once
    /// each rather than once per tag that matched.
    /// </summary>
    [Fact]
    public async Task GivenTextInBothAPayloadAndATag_WhenTheLogIsFiltered_ThenEachMatchingRowIsCountedOnce()
    {
        await using (var seed = Store())
        {
            seed.DcbEvents.Add(new DcbEventEntity
            {
                EventType = "OrderPlacedEvent:1",
                Data = "{\"reference\":\"delta\"}",
                Tags =
                [
                    new DcbEventTagEntity { Tag = "region:delta" },
                    new DcbEventTagEntity { Tag = "channel:delta" }
                ]
            });

            await seed.SaveChangesAsync();
        }

        var page = await Filtered("delta");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(1);
        page.Events.Should().HaveCount(1);
    }

    /// <summary>
    /// Case is not part of what was asked for, in a tag as in a payload.
    /// </summary>
    [Fact]
    public async Task GivenATagInAnotherCase_WhenTheLogIsFiltered_ThenTheRowStillComesBack()
    {
        var page = await Filtered("REGION:East");

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(1);
    }
}
