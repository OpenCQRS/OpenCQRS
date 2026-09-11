using System;
using System.Collections.Generic;
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
/// The two reads one stored streamed model is shown through: the events it was folded from, and the
/// snapshot it was folded into.
/// </summary>
/// <remarks>
/// A page about one model is a narrower question than the lists ask. A stream holds every model
/// written from it, so the events of one are its stream's, narrowed to the types that model applies
/// and to the events its identifier claims — the same two narrowings the store itself folds
/// through, which is what makes the count on the page agree with the version in the row.
/// <para>
/// Written through a context carrying the store's own mapping and read through the tool's, like the
/// rest of these: the tool is pointed at a store somebody else created.
/// </para>
/// </remarks>
public class SqliteStreamedModelTests : IAsyncLifetime
{
    private readonly string _file =
        Path.Combine(Path.GetTempPath(), $"memoria_web_tests_{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_file}";

    private const string Stream = "customer:alice";

    private const string Placed = "OrderPlaced:1";

    private const string Paid = "OrderPaid:1";

    private const string Noted = "NoteAdded:1";

    /// <summary>What the order the tests are about was addressed by, as the store wrote it.</summary>
    private const string StoreId = "ORD-1:1";

    private sealed class SeedContext(
        DbContextOptions<DomainDbContext> options,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
        : DomainDbContext(options, timeProvider, httpContextAccessor);

    /// <summary>
    /// A clock that moves an hour every time it is asked, because the store stamps its own dates and
    /// rows written in one go would otherwise share a millisecond.
    /// </summary>
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
        await using var seed = new SeedContext(Options(ConnectionString),
            new SteppingClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Substitute.For<IHttpContextAccessor>());

        await seed.Database.EnsureCreatedAsync();

        // One customer's stream, holding two of their orders and one event no order model applies.
        // Everything a narrowing has to tell apart is in here: another order in the same stream,
        // another type in the same stream, and another stream altogether.
        var appended = new[]
        {
            (Sequence: 0, Type: Placed, Order: "ORD-1"),
            (Sequence: 1, Type: Paid, Order: "ORD-1"),
            (Sequence: 2, Type: Placed, Order: "ORD-2"),
            (Sequence: 3, Type: Noted, Order: "ORD-1")
        };

        foreach (var (sequence, type, order) in appended)
        {
            seed.Events.Add(new EventEntity
            {
                Id = $"{Stream}:{sequence}",
                StreamId = Stream,
                EventType = type,
                Sequence = sequence,
                Data = $"{{\"OrderId\":\"{order}\",\"Reference\":\"R-{sequence}\"}}"
            });

            await seed.SaveChangesAsync();
        }

        seed.Events.Add(new EventEntity
        {
            Id = "customer:bob:0",
            StreamId = "customer:bob",
            EventType = Placed,
            Sequence = 0,
            Data = "{\"OrderId\":\"ORD-1\",\"Reference\":\"R-9\"}"
        });

        seed.Aggregates.Add(new AggregateEntity
        {
            Id = StoreId,
            StreamId = Stream,
            AggregateType = "Order:1",
            Version = 2,
            LatestEventSequence = 1,
            Data = "{\"OrderId\":\"ORD-1\",\"Paid\":42}"
        });

        seed.Projections.Add(new ProjectionEntity
        {
            Id = StoreId,
            StreamId = Stream,
            ProjectionType = "OrderSummary:1",
            Version = 2,
            LatestEventSequence = 1,
            Data = "{\"OrderId\":\"ORD-1\",\"Lines\":1}"
        });

        await seed.SaveChangesAsync();
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

    private static StreamedEventFilter EventsOf(
        IReadOnlyList<string>? types = null,
        IReadOnlyDictionary<string, string>? properties = null) =>
        new(StreamPattern: Stream, EventType: null, Text: null, Descending: false, Page: 1, Size: 20)
        {
            EventTypes = types,
            Properties = properties
        };

    /// <summary>
    /// The types a model applies narrow the stream to the events it folds — so an event of a type it
    /// applies none of stays out, however squarely it sits in the stream.
    /// </summary>
    [Fact]
    public async Task GivenAStream_WhenTheTypesAModelAppliesAreAsked_ThenOnlyThoseComeBack()
    {
        await using var context = Read();

        var page = await new EfStreamedReads(context).Events(EventsOf([Placed, Paid]));

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(3, "the stream holds two placed and one paid, and one note besides");
        page.Events.Select(appended => appended.Event.Type).Should().NotContain(Noted);
    }

    /// <summary>
    /// Several models share one stream, and what tells one of them from another is the property its
    /// identifier claims. Without this a page about one order would show the customer's others.
    /// </summary>
    [Fact]
    public async Task GivenAStream_WhenAnIdentifiersPropertyIsAsked_ThenOnlyItsOwnEventsComeBack()
    {
        await using var context = Read();

        var page = await new EfStreamedReads(context).Events(
            EventsOf([Placed, Paid], new Dictionary<string, string> { ["OrderId"] = "ORD-1" }));

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(2, "the other order's placing is in the same stream and is not this one's");
        page.Events.Select(appended => appended.Event.Position).Should().Equal(0, 1);
    }

    /// <summary>
    /// The property narrows within the stream asked for, not across the store: another customer's
    /// order carrying the same reference is another stream's business.
    /// </summary>
    [Fact]
    public async Task GivenAnotherStream_WhenItHoldsTheSameProperty_ThenItStaysOut()
    {
        await using var context = Read();

        var page = await new EfStreamedReads(context).Events(
            EventsOf(properties: new Dictionary<string, string> { ["OrderId"] = "ORD-1" }));

        using var scope = new AssertionScope();

        page.Error.Should().BeNull();
        page.Total.Should().Be(3, "three of this stream's four events carry it, and bob's is not one");
        page.Events.Select(appended => appended.StreamId).Should().AllBe(Stream);
    }

    /// <summary>
    /// A model addressed in one stream is read back whole, payload included — which is the one thing
    /// the lists deliberately do not read, and the whole of what a page about one model shows.
    /// </summary>
    [Fact]
    public async Task GivenAStoredAggregate_WhenItIsReadByItsAddress_ThenTheWholeRowComesBack()
    {
        await using var context = Read();

        var read = await new EfStreamedReads(context).Model(
            new StreamedModelAddress(StreamedModelKind.Aggregate, Stream, StoreId));

        using var scope = new AssertionScope();

        read.Error.Should().BeNull();
        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Type.Should().Be("Order:1");
        read.Snapshot.Version.Should().Be(2);
        read.Snapshot.Sequence.Should().Be(1);
        read.Snapshot.Data.Should().Be("{\"OrderId\":\"ORD-1\",\"Paid\":42}");
    }

    /// <summary>
    /// The two kinds are kept in two tables, so one address reaches a different row in each — and a
    /// page about a projection must not answer with the aggregate that shares its id.
    /// </summary>
    [Fact]
    public async Task GivenAStoredProjection_WhenItIsReadByItsAddress_ThenItIsNotTheAggregate()
    {
        await using var context = Read();

        var read = await new EfStreamedReads(context).Model(
            new StreamedModelAddress(StreamedModelKind.Projection, Stream, StoreId));

        using var scope = new AssertionScope();

        read.Error.Should().BeNull();
        read.Snapshot.Should().NotBeNull();
        read.Snapshot!.Type.Should().Be("OrderSummary:1");
        read.Snapshot.Data.Should().Contain("Lines");
    }

    /// <summary>
    /// Nothing stored under that address is an answer rather than a fault: the events may be there
    /// with no snapshot ever written over them.
    /// </summary>
    [Fact]
    public async Task GivenNothingStored_WhenItIsReadByItsAddress_ThenThereIsNoRowAndNoError()
    {
        await using var context = Read();

        var read = await new EfStreamedReads(context).Model(
            new StreamedModelAddress(StreamedModelKind.Aggregate, Stream, "ORD-404:1"));

        using var scope = new AssertionScope();

        read.Snapshot.Should().BeNull();
        read.Error.Should().BeNull();
    }
}
