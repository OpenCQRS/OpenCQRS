namespace Memoria.Web.Data;

/// <summary>
/// The database engines the tool can open a store in.
/// </summary>
/// <remarks>
/// The three relational providers the EF Core stores are built and tested against. Cosmos is not
/// among them: it is reached through its own domain service rather than a <c>DbContext</c>, and
/// there is no DCB store for it, so it is not a provider this application could swap in.
/// </remarks>
public enum DatabaseProvider
{
    /// <summary>PostgreSQL, through Npgsql.</summary>
    Npgsql,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>SQLite, against a file.</summary>
    Sqlite,

    /// <summary>
    /// Azure Cosmos DB, through its own SDK rather than a <c>DbContext</c>.
    /// </summary>
    /// <remarks>
    /// The odd one out, and deliberately so. The other three are relational providers Entity
    /// Framework Core opens a context on; this one is a document store the tool queries directly,
    /// because the store that wrote those documents does too. It also carries no dynamic
    /// consistency boundary, so only the streamed pages can be served from it.
    /// </remarks>
    Cosmos
}
