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
    Sqlite
}
