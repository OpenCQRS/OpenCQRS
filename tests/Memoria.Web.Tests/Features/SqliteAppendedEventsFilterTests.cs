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
/// One box over one column: the payload, as it was written. Worth pinning because the alternative
/// is silent — a box that also matched the position answered a search for a reference beginning
/// <c>14</c> with the fourteenth row as well, and a reader has no way to tell the extra row from a
/// real match.
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

        // Positions are the database's, so these take one, two and three in order. No payload
        // carries a digit, which is what leaves a numeric search with nothing but the position to
        // match on.
        foreach (var reference in new[] { "alpha", "beta", "gamma" })
        {
            seed.DcbEvents.Add(new DcbEventEntity
            {
                EventType = "OrderPlacedEvent:1",
                Data = $"{{\"reference\":\"{reference}\"}}"
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
        page.Total.Should().Be(0, "the box narrows by payload, and no payload carries a 2");
    }
}
