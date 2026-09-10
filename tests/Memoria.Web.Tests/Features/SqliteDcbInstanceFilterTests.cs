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
/// What the text box on the DCB aggregate and projection data pages reaches.
/// </summary>
/// <remarks>
/// One box over one column: the boundary the model was folded under. The store id is the store's
/// own key rather than anything a reader chose, and it carries the model's id and its type version
/// spliced together — so a box that also matched it answered a search for one tag value with every
/// row whose key happened to spell it, and a reader has no way to tell the extra rows from real
/// matches.
/// <para>
/// A SQLite file rather than an in-memory provider, because the narrowing is the database's: a
/// predicate that reads one way in LINQ-to-Objects and another in SQL is exactly what this is for.
/// </para>
/// </remarks>
public class SqliteDcbInstanceFilterTests : IAsyncLifetime
{
    private readonly string _file =
        Path.Combine(Path.GetTempPath(), $"memoria_web_dcb_instances_{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_file}";

    private static DbContextOptions<DcbDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<DcbDbContext>().UseSqlite(connectionString).Options;

    private DcbStoreDbContext Store() =>
        new(Options(ConnectionString), TimeProvider.System, Substitute.For<IHttpContextAccessor>());

    public async Task InitializeAsync()
    {
        await using var seed = Store();

        await seed.Database.EnsureCreatedAsync();

        // The store id carries the model's own id and the version of its binding, so it spells
        // digits and words that appear in no tag. Neither boundary here mentions "42" or "1".
        Seed(seed, storeId: "42:1", boundary: "product:alpha");
        Seed(seed, storeId: "7:1", boundary: "product:beta");

        await seed.SaveChangesAsync();
    }

    private static void Seed(DcbStoreDbContext store, string storeId, string boundary) =>
        store.DcbSnapshots.Add(new DcbSnapshotEntity
        {
            Id = $"{DcbSnapshotEntity.AggregateKind}:{storeId}:{boundary}",
            SnapshotKind = DcbSnapshotEntity.AggregateKind,
            StoreId = storeId,
            TagQuery = boundary,
            ModelType = "Product:1",
            Version = 1,
            Data = "{}"
        });

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

    private Task<InstancePage> Filtered(string text) =>
        IdentifierInstances.Page(
            Store(),
            DcbModelKind.Aggregate,
            modelType: null,
            shape: null,
            text,
            InstanceSort.Updated,
            descending: true,
            page: 1,
            size: 10);

    [Fact]
    public async Task GivenTextInABoundary_WhenTheModelsAreFiltered_ThenTheRowFoldedUnderItComesBack()
    {
        var page = await Filtered("alpha");

        using var scope = new AssertionScope();

        page.Total.Should().Be(1);
        page.Rows.Should().ContainSingle().Which.Boundary.Should().Be("product:alpha");
    }

    /// <summary>
    /// The store id is not what the box asks about, so a row is not a match merely because the key
    /// it was filed under spells the text.
    /// </summary>
    [Fact]
    public async Task GivenTextOnlyAStoreIdCarries_WhenTheModelsAreFiltered_ThenNoRowIsMatchedByItsKey()
    {
        var page = await Filtered("42");

        page.Total.Should().Be(0, "the box narrows by tag, and no boundary carries a 42");
    }
}
