using Memoria.EventSourcing.Dcb.Extensions;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Extensions;
using Memoria.EventSourcing.Extensions;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Extensions;
using Memoria.Examples.Ecommerce.Dcb.Domain;
using Memoria.Web.Components;
using Memoria.Web.Data;
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

// No AddMemoria: this application dispatches nothing. It binds the domain types of both event
// sourcing models so it can name them, and holds the two stores so it can write a snapshot back.
// Aggregate id types are not bound by either call — nothing in the framework scans for them.
builder.Services.AddMemoriaEventSourcing(typeof(App));
builder.Services.AddMemoriaEntityFrameworkCore<StreamedStoreDbContext>();

builder.Services.AddMemoriaDcb(typeof(Product));
builder.Services.AddMemoriaDcbEntityFrameworkCore<DcbStoreDbContext>();

var app = builder.Build();

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
