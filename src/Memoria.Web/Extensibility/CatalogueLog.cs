namespace Memoria.Web.Extensibility;

/// <summary>
/// What a reload of the domain types is worth saying about itself.
/// </summary>
/// <remarks>
/// Here rather than beside any one caller because the registry is reloaded from three places — once
/// at start-up, and again on every upload, removal or refresh — and all three want to say the same
/// thing about what came back.
/// </remarks>
public static class CatalogueLog
{
    /// <summary>
    /// Writes how many types a reload found, and one line for each assembly that could not be read.
    /// </summary>
    /// <param name="logger">Where the lines go.</param>
    /// <param name="catalogue">What the reload came back with.</param>
    public static void LogCatalogue(this ILogger logger, DomainTypeCatalogue catalogue)
    {
        logger.LogInformation("Registered {TypeCount} domain type(s). {ErrorCount} problem(s).",
            catalogue.Count, catalogue.Errors.Count);

        foreach (var error in catalogue.Errors)
        {
            logger.LogWarning("Extension problem: {Error}", error);
        }
    }
}
