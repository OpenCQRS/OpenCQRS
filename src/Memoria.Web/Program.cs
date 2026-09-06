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

// The domain types uploaded through the settings page. Bound here, before the stores, for the
// reason given on AddDomainExtensions.
var extensionStore = new ExtensionStore(
    builder.Configuration["Extensions:Directory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "extensions"));

var catalogue = builder.Services.AddDomainExtensions(extensionStore);

builder.Services.AddMemoriaEntityFrameworkCore<StreamedStoreDbContext>();
builder.Services.AddMemoriaDcbEntityFrameworkCore<DcbStoreDbContext>();

var app = builder.Build();

app.Logger.LogInformation("Loaded {TypeCount} domain types from uploaded assemblies. {ErrorCount} problem(s).",
    catalogue.Count, catalogue.Errors.Count);

foreach (var error in catalogue.Errors)
{
    app.Logger.LogWarning("Extension problem: {Error}", error);
}

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

// What the page that is waiting for a restart polls. Answering at all is the whole signal.
app.MapGet("/health", () => Results.Ok("ok"));

// A plain form post rather than a Blazor upload: the settings page renders statically like the
// rest, and the response has to outlive the shutdown it triggers. Antiforgery still applies —
// binding the form marks the endpoint, and the form carries the token via <AntiforgeryToken/>.
app.MapPost("/settings/upload", (
    HttpContext context,
    IHostApplicationLifetime lifetime,
    ExtensionStore store,
    ILoggerFactory loggerFactory,
    [FromForm] IFormFileCollection files) =>
{
    var logger = loggerFactory.CreateLogger("Memoria.Web.Upload");

    if (files.Count == 0)
    {
        return Results.LocalRedirect("/settings?error=Choose%20at%20least%20one%20zip%20file.");
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

            var reason = $"{file.FileName} could not be installed: {exception.Message}";
            return Results.LocalRedirect($"/settings?error={Uri.EscapeDataString(reason)}");
        }
    }

    // Stopped only once this response is on the wire, so the page doing the waiting is the last
    // thing the dying process serves. Nothing here starts it again: a process manager does that,
    // and under a plain `dotnet run` it stays down.
    context.Response.OnCompleted(() =>
    {
        lifetime.StopApplication();
        return Task.CompletedTask;
    });

    return Results.Content(RestartingPage.Html, "text/html");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
