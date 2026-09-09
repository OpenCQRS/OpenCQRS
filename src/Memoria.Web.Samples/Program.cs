using Memoria;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.EventSourcing.Dcb.Extensions;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
using Memoria.EventSourcing.Extensions;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Extensions;
using Memoria.Extensions;
using Memoria.Web.Data;
using Memoria.Web.Samples.Data;
using Memoria.Web.Samples.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// The sample domain types the web tool loads, registered the way Memoria.Web registers them so
// that anything written under Streamed/ and Dcb/ is exercised here first, against the same store.

// The content root is the output directory rather than whatever directory the process was
// started from, so appsettings.json is found by `dotnet run` from the repository root too.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// The provider is read off the connection string the same way the tool reads its own, so seeding
// reaches whichever store the tool was pointed at rather than assuming Postgres.
var database = DatabaseConnection.Of(
    builder.Configuration.GetConnectionString(DatabaseConnection.Name),
    builder.Configuration[DatabaseConnection.Setting]);

// Both contexts take their options as the base type's DbContextOptions rather than their own
// closed type, so each is registered against that.
builder.Services.AddScoped(serviceProvider =>
{
    var options = new DbContextOptionsBuilder<DomainDbContext>();
    database.Apply(options).UseApplicationServiceProvider(serviceProvider);
    return options.Options;
});

builder.Services.AddScoped(serviceProvider =>
{
    var options = new DbContextOptionsBuilder<DcbDbContext>();
    database.Apply(options).UseApplicationServiceProvider(serviceProvider);
    return options.Options;
});

builder.Services.AddDbContext<StreamedStoreDbContext>(options => database.Apply(options));
builder.Services.AddDbContext<DcbStoreDbContext>(options => database.Apply(options));

builder.Services.AddMemoria(typeof(Program));

// The two event sourcing models side by side. Each store call replaces the default no-op service
// its model registers, so it comes after.
builder.Services.AddMemoriaEventSourcing(typeof(Program));
builder.Services.AddMemoriaDcb(typeof(Program));

builder.Services.AddMemoriaEntityFrameworkCore<StreamedStoreDbContext>();
builder.Services.AddMemoriaDcbEntityFrameworkCore<DcbStoreDbContext>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var streamedContext = services.GetRequiredService<StreamedStoreDbContext>();
var dcbContext = services.GetRequiredService<DcbStoreDbContext>();

Console.WriteLine("Memoria.Web.Samples");
Console.WriteLine($"  database : {streamedContext.Database.GetDbConnection().Database}");
Console.WriteLine($"  bound    : {Count(TypeBindings.EventTypeBindings)} events, " +
                  $"{Count(TypeBindings.AggregateTypeBindings)}+{Count(DcbTypeBindings.AggregateTypeBindings)} aggregates, " +
                  $"{Count(TypeBindings.ProjectionTypeBindings)}+{Count(DcbTypeBindings.ProjectionTypeBindings)} projections " +
                  "(streamed+dcb)");

// The samples have a database to themselves, which may not exist yet and will have no tables in it
// the first time. Both stores are installed before anything asks a question, so a deletion has
// something to delete from.
try
{
    if (await StoreSchema.Ensure(streamedContext, "DomainAggregates") |
        await StoreSchema.Ensure(dcbContext, "DcbSnapshots"))
    {
        Console.WriteLine("  schema   : installed");
    }
}
catch (Exception exception) when (exception is not OperationCanceledException)
{
    // Almost always a server that is not running. Said plainly, because a page of Npgsql stack
    // trace is a poor way to be told to start PostgreSQL.
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Could not reach the database: {exception.GetBaseException().Message}");
    Console.Error.WriteLine("Check the 'Memoria' connection string in appsettings.json and that the server is up.");

    return 1;
}

var action = Menu.AskForAction();

if (action == SampleDataAction.None)
{
    Console.WriteLine("Nothing chosen. Nothing done.");
    return 0;
}

// A deletion on its own clears both stores; a deletion before a write clears what is about to be
// written, so that the run leaves the store holding exactly what it just put there.
var scopeOfRun = action == SampleDataAction.DeleteAll
    ? SampleDataScope.Both
    : Menu.AskForScope();

if (scopeOfRun == SampleDataScope.None)
{
    Console.WriteLine("Nothing chosen. Nothing done.");
    return 0;
}

if (action is SampleDataAction.ReplaceAll or SampleDataAction.DeleteAll)
{
    await Delete(scopeOfRun);
}

if (action is SampleDataAction.Add or SampleDataAction.ReplaceAll)
{
    await Seed(scopeOfRun);
}

return 0;

async Task Delete(SampleDataScope what)
{
    Console.WriteLine();

    if (what.HasFlag(SampleDataScope.Streamed))
    {
        Report(await SampleDataEraser.EraseStreamed(streamedContext));
    }

    if (what.HasFlag(SampleDataScope.Dcb))
    {
        Report(await SampleDataEraser.EraseDcb(dcbContext));
    }

    return;

    static void Report(IReadOnlyList<(string Table, int Rows)> deleted)
    {
        foreach (var (table, rows) in deleted)
        {
            Console.WriteLine($"deleted {rows,7} from {table}");
        }
    }
}

async Task Seed(SampleDataScope what)
{
    var random = new Random();
    var time = services.GetRequiredService<TimeProvider>();
    var report = new SeedReport();

    if (what.HasFlag(SampleDataScope.Streamed))
    {
        await StreamedSampleData.Add(services.GetRequiredService<IDomainService>(), random, time, report);
    }

    if (what.HasFlag(SampleDataScope.Dcb))
    {
        await DcbSampleData.Add(services.GetRequiredService<IDcbDomainService>(), random, report);
    }

    await report.Print();
}

int Count(Dictionary<string, Type> bindings) =>
    bindings.Count(binding => binding.Value.Assembly == typeof(Program).Assembly);

/// <summary>
/// Named so that <c>typeof(Program)</c> can point the registration scans at this assembly, which a
/// top-level program's implicit entry point class cannot do from outside.
/// </summary>
public partial class Program;
