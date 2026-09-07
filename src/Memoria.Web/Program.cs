using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Extensions;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
using Memoria.EventSourcing.Extensions;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Extensions;
using Memoria.Extensions;
using Memoria.Web.Components;
using Memoria.Web.Data;
using Memoria.Web.Extensibility;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// The domain types uploaded through the settings page. Only registered here — the assemblies are
// read below, and again whenever someone uploads or asks for a refresh.
builder.Services.AddDomainExtensions(
    new ExtensionStore(
        builder.Configuration["Extensions:Directory"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "extensions")),
    typeof(Program).Assembly);

var app = builder.Build();

var registry = app.Services.GetRequiredService<DomainTypeRegistry>();
registry.Reload();
LogCatalogue(app.Logger, registry.Current);

// EF Core builds its model and compiles each distinct query the first time it meets it, and Npgsql
// opens its first connection then too — close to a second of work, which whoever opens the first
// page that reads anything would otherwise pay. Warmed here instead, in the background so the
// application starts serving straight away, and quietly, because a store that cannot be reached is
// the page's problem to report rather than a reason not to start.
_ = Task.Run(() => WarmStore(app));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Plain form posts rather than Blazor forms: the settings page renders statically like the rest.
// Antiforgery still applies — binding the form marks the endpoint, and each form carries the token
// via <AntiforgeryToken/>.
app.MapPost("/settings/upload", (
    DomainTypeRegistry types,
    ExtensionStore store,
    ILoggerFactory loggerFactory,
    [FromForm] IFormFileCollection files) =>
{
    var logger = loggerFactory.CreateLogger("Memoria.Web.Settings");

    if (files.Count == 0)
    {
        return Back(error: "Choose at least one zip file.");
    }

    foreach (var file in files)
    {
        try
        {
            using var content = file.OpenReadStream();
            store.Install(file.FileName, content);
            logger.LogInformation("Installed {FileName}.", file.FileName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not install {FileName}.", file.FileName);
            return Back(error: $"{file.FileName} could not be installed: {exception.Message}");
        }
    }

    types.Reload();
    LogCatalogue(logger, types.Current);

    return Back(message: $"Uploaded {files.Count} file(s). {types.Current.Count} type(s) registered.");
});

app.MapPost("/settings/refresh", async (
    HttpContext context,
    IAntiforgery antiforgery,
    DomainTypeRegistry types,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Memoria.Web.Settings");

    // This endpoint binds no form field, so the antiforgery middleware does not treat it as a form
    // post and would let it through unchecked. Refreshing changes what every user resolves, so it
    // is checked here instead.
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Back(error: "That request could not be verified. Reload the page and try again.");
    }

    types.Reload();
    LogCatalogue(logger, types.Current);

    return Back(message: $"{types.Current.Count} type(s) registered.");
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
return;

// Back to the settings page carrying what happened, so the outcome survives the redirect.
static IResult Back(string? message = null, string? error = null)
{
    var query = message is not null
        ? $"?message={Uri.EscapeDataString(message)}"
        : $"?error={Uri.EscapeDataString(error ?? string.Empty)}";

    return Results.LocalRedirect($"/settings{query}");
}

static void LogCatalogue(ILogger logger, DomainTypeCatalogue catalogue)
{
    logger.LogInformation("Registered {TypeCount} domain type(s). {ErrorCount} problem(s).",
        catalogue.Count, catalogue.Errors.Count);

    foreach (var error in catalogue.Errors)
    {
        logger.LogWarning("Extension problem: {Error}", error);
    }
}

/// <summary>
/// Runs the list page's own query once, so nobody's first page load pays for building the model
/// and compiling it.
/// </summary>
/// <remarks>
/// The real query rather than a stand-in: EF compiles each distinct shape separately, so a simpler
/// warming query builds the model but leaves the page's own plan to be compiled when it is asked
/// for. Both orders and the filtered form are warmed alongside it, since those are what the column
/// headings and the filter box lead to. Failure is logged and dropped — the store being unreachable
/// is something the pages report, not a reason to hold up start-up.
/// </remarks>
static async Task WarmStore(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IDcbDbContext>();
        var types = app.Services.GetRequiredService<DomainTypeRegistry>().Current;

        foreach (var aggregate in types.DcbAggregates)
        {
            var shape = DomainTypeDescriber.Describe(aggregate, types.DcbAggregateIds).Identifiers
                .Select(IdentifierShape.Of)
                .FirstOrDefault(candidate => candidate is not null);

            if (shape is null)
            {
                continue;
            }

            var modelType = DcbTypeBindings.GetAggregateBindingKey(aggregate);
            IReadOnlyList<Instance> listed = [];

            foreach (var sort in Enum.GetValues<InstanceSort>())
            {
                foreach (var tag in new string?[] { null, "warm" })
                {
                    var page = await IdentifierInstances.Page(store, shape, modelType, tag, sort,
                        descending: true, page: 1, size: InstanceQuery.DefaultPageSize);

                    listed = listed.Count > 0 ? listed : page.Rows;
                }
            }

            // The detail page reads through the domain service instead, which has queries of its
            // own to compile — folding one real aggregate warms those the same way.
            if (listed.FirstOrDefault() is { } instance &&
                IdentifierFactory.Create(shape.Identifier, instance.Values.ToDictionary(
                    value => value.Key, value => (string?)value.Value)).Instance is { } identifier)
            {
                await AggregateReader.Load(
                    scope.ServiceProvider.GetRequiredService<IDcbDomainService>(),
                    aggregate,
                    identifier,
                    ReadMode.SnapshotWithNewEventsOrCreate);
            }

            app.Logger.LogInformation("Store warmed on {Aggregate}.", aggregate.Name);
            return;
        }

        // Nothing uploaded yet, so there is no real query to run. The model is still worth building.
        await store.DcbSnapshots.AsNoTracking().Select(snapshot => snapshot.Id).FirstOrDefaultAsync();

        app.Logger.LogInformation("Store warmed.");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception,
            "Could not warm the store. The first page that reads it will be slower.");
    }
}
