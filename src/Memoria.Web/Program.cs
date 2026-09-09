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

// Which engine the store is in is read off the connection string, or taken from Database:Provider
// where the string could be more than one. The tool is pointed at a store somebody else created,
// so it is told rather than assuming Postgres.
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

// The domain types uploaded through the settings page. Only registered here — the assemblies are
// read below, and again whenever someone uploads or asks for a refresh.
builder.Services.AddDomainExtensions(
    new ExtensionStore(
        builder.Configuration["Extensions:Directory"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "extensions")),
    typeof(Program).Assembly);

var app = builder.Build();

// Logged because the provider is now read rather than fixed: a store that answers nothing is the
// first thing anyone will suspect the connection string of, and this says how it was read.
app.Logger.LogInformation("Store opened with {Provider}.", database.Provider);

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

app.MapPost("/settings/delete", (
    DomainTypeRegistry types,
    ExtensionStore store,
    ILoggerFactory loggerFactory,
    [FromForm] string name) =>
{
    var logger = loggerFactory.CreateLogger("Memoria.Web.Settings");

    try
    {
        store.Remove(name);
        logger.LogInformation("Removed {FileName}.", name);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Could not remove {FileName}.", name);
        return Back(error: $"{name} could not be removed: {exception.Message}");
    }

    types.Reload();
    LogCatalogue(logger, types.Current);

    return Back(message: $"Removed {name}. {types.Current.Count} type(s) registered.");
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
        return Back(error: "That request could not be verified. Reload the page and try again.",
            tab: "types");
    }

    types.Reload();
    LogCatalogue(logger, types.Current);

    return Back(message: $"{types.Current.Count} type(s) registered.", tab: "types");
}).DisableAntiforgery();

// The one write the DCB pages offer, and the same one for each of the two models. A form post
// rather than an interactive component, so the detail pages stay statically rendered like the rest
// of them — and a POST rather than a link, because it writes a snapshot.
app.MapPost("/dcb/aggregates/update", async (
    DomainTypeRegistry types,
    IDcbDomainService store,
    ILoggerFactory loggerFactory,
    HttpRequest request,
    [FromForm] string type,
    [FromForm] string id,
    [FromForm] string returnUrl) =>
    await Refresh(DcbModelKind.Aggregate, types, store, loggerFactory, request, type, id, returnUrl));

app.MapPost("/dcb/projections/update", async (
    DomainTypeRegistry types,
    IDcbDomainService store,
    ILoggerFactory loggerFactory,
    HttpRequest request,
    [FromForm] string type,
    [FromForm] string id,
    [FromForm] string returnUrl) =>
    await Refresh(DcbModelKind.Projection, types, store, loggerFactory, request, type, id, returnUrl));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
return;

// Back to the settings page carrying what happened, so the outcome survives the redirect.
// The tab is carried back with the message because the settings page writes each one under the
// button that produced it: an upload's answer belongs on the installed tab, a refresh's on the
// types tab, and landing on the other one would leave the answer where it was not asked for.
static IResult Back(string? message = null, string? error = null, string tab = "installed")
{
    var query = message is not null
        ? $"?message={Uri.EscapeDataString(message)}"
        : $"?error={Uri.EscapeDataString(error ?? string.Empty)}";

    return Results.LocalRedirect($"/settings{query}&tab={tab}");
}

/// <summary>
/// Brings one model's snapshot up to date and goes back to the page the button was pressed on,
/// carrying what happened.
/// </summary>
/// <remarks>
/// One handler for aggregates and projections, because the two differ only in which list the posted
/// names are matched against and which of the store's two update methods is called — and the second
/// of those the refresher reads off the identifier rather than being told.
/// </remarks>
static async Task<IResult> Refresh(
    DcbModelKind kind,
    DomainTypeRegistry types,
    IDcbDomainService store,
    ILoggerFactory loggerFactory,
    HttpRequest request,
    string type,
    string id,
    string returnUrl)
{
    var logger = loggerFactory.CreateLogger("Memoria.Web.Dcb");

    // Local: the return address arrives on the form, so it may not send anyone off-site.
    IResult BackToModel(string? message = null, string? error = null)
    {
        var separator = returnUrl.Contains('?') ? "&" : "?";
        var carried = message is not null
            ? $"message={Uri.EscapeDataString(message)}"
            : $"error={Uri.EscapeDataString(error ?? string.Empty)}";

        return Results.LocalRedirect($"{returnUrl}{separator}{carried}");
    }

    // Matched against what is registered, exactly as the page matches them, so a name posted here
    // can only ever reach a type this application already knows about — and only one of the two
    // kinds, so a projection cannot be refreshed through the aggregates' address.
    var model = DomainTypeDescriber.Select(types.Current.Models(kind), type);

    var identifierType = model is null
        ? null
        : DomainTypeDescriber.Describe(model, types.Current.Identifiers(kind))
            .Identifiers.FirstOrDefault(candidate => candidate.FullName == id);

    if (model is null || identifierType is null)
    {
        return BackToModel(error: $"That {kind.ToString().ToLowerInvariant()} and identifier are no longer registered.");
    }

    // The identifier's own values, posted under the names its constructor takes — the same shape
    // the address carries them in.
    var values = request.Form.ToDictionary(
        field => field.Key, field => (string?)field.Value.LastOrDefault(),
        StringComparer.OrdinalIgnoreCase);

    var created = IdentifierFactory.Create(identifierType, values);

    if (created.Instance is null)
    {
        return BackToModel(error: created.Error ?? "That identifier could not be built.");
    }

    var refreshed = await ModelRefresher.Refresh(store, model, created.Instance);

    if (refreshed.Error is not null)
    {
        logger.LogWarning("Could not refresh {Model}: {Error}", model.Name, refreshed.Error);
        return BackToModel(error: refreshed.Error);
    }

    logger.LogInformation("Refreshed the snapshot for {Model}.", model.Name);

    return BackToModel(message: refreshed.Refreshed
        ? "Snapshot refreshed."
        : $"Nothing to refresh — no snapshot, and no events inside the boundary this " +
          $"{kind.ToString().ToLowerInvariant()} applies.");
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

        // Either kind will do. Both pages run the same two queries, and the kind rides in as a
        // parameter rather than as part of the SQL, so whichever is warmed first warms the other's
        // pages too — and an application with only projections uploaded is still warmed.
        foreach (var kind in Enum.GetValues<DcbModelKind>())
        {
            foreach (var model in types.Models(kind))
            {
                var shape = DomainTypeDescriber.Describe(model, types.Identifiers(kind)).Identifiers
                    .Select(IdentifierShape.Of)
                    .FirstOrDefault(candidate => candidate is not null);

                if (shape is null)
                {
                    continue;
                }

                // Read off the attribute rather than through the framework's own lookup, which
                // throws for a type carrying none. A model that cannot be stored has no rows to
                // warm, so it is passed over rather than allowed to fail the warming of the rest.
                if (DomainTypeDescriber.BindingOf(model)?.Key is not { } modelType)
                {
                    continue;
                }

                IReadOnlyList<Instance> listed = [];

                foreach (var sort in Enum.GetValues<InstanceSort>())
                {
                    foreach (var tag in new string?[] { null, "warm" })
                    {
                        // The three ways the list narrows: to one identifier's rows, to one model's
                        // whatever addresses them, and to every model of the kind. Each leaves out
                        // a different clause, so each is its own query for EF to compile — and the
                        // page opens on the widest of them, which would otherwise be the one left
                        // to compile on arrival.
                        foreach (var (narrowed, narrowedShape) in new (string?, IdentifierShape?)[]
                                 {
                                     (modelType, shape), (modelType, null), (null, null)
                                 })
                        {
                            var page = await IdentifierInstances.Page(store, kind, narrowed,
                                narrowedShape, tag, sort, descending: true, page: 1,
                                size: InstanceQuery.DefaultPageSize);

                            // Only the shaped read unfolds a boundary into the values an identifier
                            // is built from, which is what the detail read below needs.
                            if (narrowedShape is not null)
                            {
                                listed = listed.Count > 0 ? listed : page.Rows;
                            }
                        }
                    }
                }

                // The detail page reads the same table, but addressed by one whole boundary rather
                // than by the shape of one. That is a distinct query for EF to compile, so warming
                // the list alone would leave it to be compiled on the first row anyone opens.
                if (listed.FirstOrDefault() is { } instance &&
                    DcbModels.BoundaryOf(IdentifierFactory.Create(shape.Identifier,
                        instance.Values.ToDictionary(
                            value => value.Key, value => (string?)value.Value)).Instance) is { } boundary)
                {
                    await ModelReader.Load(store, model, kind, modelType, boundary.ToString());
                }

                app.Logger.LogInformation("Store warmed on {Model}.", model.Name);
                return;
            }
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
