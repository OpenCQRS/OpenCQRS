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
using Memoria.Web.Samples.Data;
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

var connectionString = builder.Configuration.GetConnectionString("Memoria")
                       ?? throw new InvalidOperationException(
                           "Connection string 'Memoria' is not configured in appsettings.json.");

// Both contexts take their options as the base type's DbContextOptions rather than their own
// closed type, so each is registered against that.
builder.Services.AddScoped(serviceProvider => new DbContextOptionsBuilder<DomainDbContext>()
    .UseNpgsql(connectionString)
    .UseApplicationServiceProvider(serviceProvider)
    .Options);

builder.Services.AddScoped(serviceProvider => new DbContextOptionsBuilder<DcbDbContext>()
    .UseNpgsql(connectionString)
    .UseApplicationServiceProvider(serviceProvider)
    .Options);

builder.Services.AddDbContext<StreamedStoreDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<DcbStoreDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMemoria(typeof(Program));

// The two event sourcing models side by side. Each store call replaces the default no-op service
// its model registers, so it comes after.
builder.Services.AddMemoriaEventSourcing(typeof(Program));
builder.Services.AddMemoriaDcb(typeof(Program));

builder.Services.AddMemoriaEntityFrameworkCore<StreamedStoreDbContext>();
builder.Services.AddMemoriaDcbEntityFrameworkCore<DcbStoreDbContext>();

var host = builder.Build();

using var scope = host.Services.CreateScope();

var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
var domainService = scope.ServiceProvider.GetRequiredService<IDomainService>();
var dcbDomainService = scope.ServiceProvider.GetRequiredService<IDcbDomainService>();

Console.WriteLine("Memoria.Web.Samples");
Console.WriteLine($"  dispatcher      : {dispatcher.GetType().Name}");
Console.WriteLine($"  streamed store  : {domainService.GetType().Name}");
Console.WriteLine($"  dcb store       : {dcbDomainService.GetType().Name}");
Console.WriteLine();

// What the registration scans actually bound. The web tool reads the same attributes out of this
// assembly when it is uploaded, so a type missing here is a type that would not show up there
// either — which makes this the cheapest check that the samples are shaped right.
// Events are shared between the two models, so there is one map of them. Aggregates and
// projections are not, so there are two of each — which is why a name may be used by both models.
Write("events", TypeBindings.EventTypeBindings);
Write("streamed aggregates", TypeBindings.AggregateTypeBindings);
Write("streamed projections", TypeBindings.ProjectionTypeBindings);
Write("dcb aggregates", DcbTypeBindings.AggregateTypeBindings);
Write("dcb projections", DcbTypeBindings.ProjectionTypeBindings);

Console.WriteLine();
Console.WriteLine("Streamed/ folds these from a stream; Dcb/ folds them from tags. Nothing has been");
Console.WriteLine("written to the store — point the samples at one and drive them from here.");

return;

void Write(string what, Dictionary<string, Type> bindings)
{
    var mine = bindings
        .Where(binding => binding.Value.Assembly == typeof(Program).Assembly)
        .OrderBy(binding => binding.Key, StringComparer.Ordinal)
        .ToList();

    Console.WriteLine($"{mine.Count} {what}");

    foreach (var binding in mine)
    {
        Console.WriteLine($"  {binding.Key,-32} {binding.Value.FullName}");
    }
}

/// <summary>
/// Named so that <c>typeof(Program)</c> can point the registration scans at this assembly, which a
/// top-level program's implicit entry point class cannot do from outside.
/// </summary>
public partial class Program;
