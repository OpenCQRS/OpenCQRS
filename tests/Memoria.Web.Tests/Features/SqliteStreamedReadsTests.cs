using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Entities;
using Memoria.Web.Data;
using Memoria.Web.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The streamed pages read from a SQLite store.
/// </summary>
/// <remarks>
/// <para>
/// SQLite keeps a <c>DateTimeOffset</c> as text and Entity Framework Core refuses to order by one,
/// so every list page in this tool came back empty against a SQLite store with an alert where the
/// table should be. That is what these pin.
/// </para>
/// <para>
/// The rows are written through a context carrying the store's own mapping and read through the
/// tool's, because the tool is pointed at a store somebody else created: a test that wrote and read
/// through the same mapping would prove the two agree with each other and nothing about the data
/// already on disk.
/// </para>
/// <para>
/// No emulator and no server — a SQLite file in the temporary directory — so unlike the Cosmos
/// tests these run everywhere, including CI.
/// </para>
/// </remarks>
public class SqliteStreamedReadsTests : IAsyncLifetime
{
    private readonly string _file =
        Path.Combine(Path.GetTempPath(), $"memoria_web_tests_{Guid.NewGuid():N}.db");

    private const int Events = 12;

    private string ConnectionString => $"Data Source={_file}";

    private DateTimeOffset _start;

    /// <summary>A context with the store's own mapping, to write what the store would have.</summary>
    private sealed class SeedContext(
        DbContextOptions<DomainDbContext> options,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
        : DomainDbContext(options, timeProvider, httpContextAccessor);

    /// <summary>
    /// A clock that moves an hour every time it is asked.
    /// </summary>
    /// <remarks>
    /// The store stamps its own dates: <c>AuditInterceptor</c> overwrites whatever a caller put on
    /// <c>CreatedDate</c> with the current instant, so rows written in one go all land on the same
    /// millisecond and nothing can be asserted about their order. Moving the clock between saves is
    /// how these rows get the distinct, known dates the store would have given them over time.
    /// </remarks>
    private sealed class SteppingClock(DateTimeOffset start) : TimeProvider
    {
        private int _asked;

        public override DateTimeOffset GetUtcNow() => start.AddHours(_asked++);
    }

    private static DbContextOptions<DomainDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<DomainDbContext>().UseSqlite(connectionString).Options;

    private StreamedStoreDbContext Read() =>
        new(Options(ConnectionString), TimeProvider.System, Substitute.For<IHttpContextAccessor>());

    public async Task InitializeAsync()
    {
        _start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await using var seed = new SeedContext(Options(ConnectionString), new SteppingClock(_start),
            Substitute.For<IHttpContextAccessor>());

        await seed.Database.EnsureCreatedAsync();

        // One save each, so the clock moves between them and every row carries its own date — the
        // dates the audit interceptor stamps, which is what a real store holds.
        for (var index = 0; index < Events; index++)
        {
            var stream = $"customer:c-000{index % 3}";

            seed.Events.Add(new EventEntity
            {
                Id = $"{stream}:{index / 3}",
                StreamId = stream,
                EventType = "OrderPlacedEvent:1",
                Sequence = index / 3,
                Data = $"{{\"reference\":\"ORD-{index}\"}}"
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

    private static StreamedEventFilter Filter(bool descending = true, int page = 1, int size = 5) =>
        new(StreamPattern: null, EventType: null, Text: null, descending, page, size);

    [Fact]
    public async Task GivenASqliteStore_WhenTheLogIsRead_ThenItComesBackNewestFirst()
    {
        await using var context = Read();

        var page = await new EfStreamedReads(context).Events(Filter());

        using var scope = new AssertionScope();

        page.Error.Should().BeNull("ordering by a date is the whole of what a log page does");
        page.Total.Should().Be(Events);
        page.Events.Should().HaveCount(5);
        page.Events[0].Event.Written.Should().Be(_start.AddHours(Events - 1));
        page.Events.Select(appended => appended.Event.Written).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GivenASqliteStore_WhenAscendingIsAsked_ThenTheOldestComeFirst()
    {
        await using var context = Read();

        var page = await new EfStreamedReads(context).Events(Filter(descending: false));

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Events[0].Event.Written.Should().Be(_start);
        page.Events.Select(appended => appended.Event.Written).Should().BeInAscendingOrder();
    }

    /// <summary>
    /// Paging is where a mis-ordered read shows itself: the boundary between two pages is only
    /// stable if the order is.
    /// </summary>
    [Fact]
    public async Task GivenASqliteStore_WhenThePagesAreWalked_ThenNoRowIsSeenTwiceOrMissed()
    {
        await using var context = Read();

        var reads = new EfStreamedReads(context);
        var seen = new System.Collections.Generic.List<DateTimeOffset>();

        for (var page = 1; page <= 3; page++)
        {
            var read = await reads.Events(Filter(page: page, size: 5));

            read.Error.Should().BeNull();
            seen.AddRange(read.Events.Select(appended => appended.Event.Written));
        }

        using var scope = new AssertionScope();

        seen.Should().HaveCount(Events, "twelve events at five to a page fill three");
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GivenASqliteStore_WhenTheStoredModelsAreRead_ThenTheyOrderToo()
    {
        await using var seed = new SeedContext(Options(ConnectionString),
            new SteppingClock(_start.AddDays(1)), Substitute.For<IHttpContextAccessor>());

        for (var index = 0; index < 3; index++)
        {
            seed.Aggregates.Add(new AggregateEntity
            {
                Id = $"account-c-000{index}:1",
                StreamId = $"customer:c-000{index}",
                AggregateType = "CustomerAccount:1",
                Version = 1,
                LatestEventSequence = 1,
                Data = "{}"
            });

            await seed.SaveChangesAsync();
        }

        await using var context = Read();

        var page = await new EfStreamedReads(context).Snapshots(new StreamedSnapshotFilter(
            StreamedModelKind.Aggregate, StreamPattern: null, ModelType: null,
            IdentifierPattern: null, Text: null, InstanceSort.Updated, Descending: true,
            Page: 1, Size: 10));

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(3);
        page.Snapshots[0].StoreId.Should().Be("account-c-0002:1", "the newest first");
    }
}
