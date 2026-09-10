using Memoria.EventSourcing.Dcb;
using Memoria.Web.Components;
using Memoria.Web.Extensibility;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Memoria.Web.Endpoints;

// Inside the namespace, and aliased at all, because this file sits under Memoria, where a plain
// Results binds to the framework's own namespace of that name rather than to the class the
// redirects below are built with. An alias outside the namespace would still lose to it.
using Results = Microsoft.AspNetCore.Http.Results;

/// <summary>
/// Everything this application answers on: the pages themselves, and the handful of form posts the
/// statically rendered ones send.
/// </summary>
/// <remarks>
/// The posts are plain minimal-API endpoints rather than interactive components because the pages
/// that send them render statically like the rest. Antiforgery still applies — binding a form marks
/// the endpoint, and each form carries the token via <c>&lt;AntiforgeryToken/&gt;</c>.
/// </remarks>
public static class EndpointRegistration
{
    /// <summary>
    /// Maps the components, their static assets, and the writes the pages post to.
    /// </summary>
    /// <param name="app">The application.</param>
    public static WebApplication MapPages(this WebApplication app)
    {
        app.MapSettings();
        app.MapDcbModels();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    /// <summary>
    /// The three writes the settings page offers: installing uploads, removing one, and rereading
    /// what is installed.
    /// </summary>
    private static void MapSettings(this WebApplication app)
    {
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
            logger.LogCatalogue(types.Current);

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
            logger.LogCatalogue(types.Current);

            return Back(message: $"Removed {name}. {types.Current.Count} type(s) registered.");
        });

        app.MapPost("/settings/refresh", async (
            HttpContext context,
            IAntiforgery antiforgery,
            DomainTypeRegistry types,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Memoria.Web.Settings");

            // This endpoint binds no form field, so the antiforgery middleware does not treat it as a
            // form post and would let it through unchecked. Refreshing changes what every user
            // resolves, so it is checked here instead.
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
            logger.LogCatalogue(types.Current);

            return Back(message: $"{types.Current.Count} type(s) registered.", tab: "types");
        }).DisableAntiforgery();
    }

    /// <summary>
    /// The one write the DCB pages offer, and the same one for each of the two models.
    /// </summary>
    /// <remarks>
    /// A form post rather than an interactive component, so the detail pages stay statically
    /// rendered like the rest of them — and a POST rather than a link, because it writes a snapshot.
    /// </remarks>
    private static void MapDcbModels(this WebApplication app)
    {
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
    }

    // Back to the settings page carrying what happened, so the outcome survives the redirect.
    // The tab is carried back with the message because the settings page writes each one under the
    // button that produced it: an upload's answer belongs on the installed tab, a refresh's on the
    // types tab, and landing on the other one would leave the answer where it was not asked for.
    private static IResult Back(string? message = null, string? error = null, string tab = "installed")
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
    /// One handler for aggregates and projections, because the two differ only in which list the
    /// posted names are matched against and which of the store's two update methods is called — and
    /// the second of those the refresher reads off the identifier rather than being told.
    /// </remarks>
    private static async Task<IResult> Refresh(
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
}
