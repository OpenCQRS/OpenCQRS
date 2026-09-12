using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// Makes sure the database and both stores' tables are there before anything tries to write to them.
/// </summary>
/// <remarks>
/// <para>
/// The samples have a database of their own, which starts out empty, so something has to create the
/// schema. <c>EnsureCreated</c> cannot: it creates tables only when the database has none at all, so
/// calling it for both contexts would build the first store's tables and silently skip the second's.
/// Each context is therefore asked separately whether its own tables are there, and only the ones
/// that are missing are created.
/// </para>
/// <para>
/// Whether the database exists and how its tables are created is the provider's own business, so
/// those follow whichever engine the connection string named without anything here saying which.
/// Asking whether one table exists is the only step with no provider-neutral form, and
/// <see cref="HasTable"/> is where that is handled. The published alternative is to run the install
/// scripts under <c>scripts/install</c> by hand; see <c>docs/guides/install-the-store-schema.md</c>.
/// </para>
/// </remarks>
public static class StoreSchema
{
    /// <summary>
    /// Creates the database if it is not there, then this context's tables if they are not.
    /// </summary>
    /// <param name="context">The context whose model describes the tables.</param>
    /// <param name="witnessTable">
    /// A table from that model. Its presence is taken to mean the whole store is installed, which is
    /// true of anything these two contexts create together.
    /// </param>
    /// <returns>Whether anything was created.</returns>
    public static async Task<bool> Ensure(DbContext context, string witnessTable)
    {
        var creator = context.Database.GetService<IRelationalDatabaseCreator>();

        var created = false;

        if (!await creator.ExistsAsync())
        {
            await creator.CreateAsync();
            created = true;
        }

        if (await HasTable(context, witnessTable))
        {
            return created;
        }

        await creator.CreateTablesAsync();

        return true;
    }

    /// <summary>
    /// Whether the database holds a table of this name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two questions rather than three. PostgreSQL and SQL Server both answer from the standard's
    /// <c>INFORMATION_SCHEMA</c>, so they share one; SQLite has no such thing and keeps its schema in
    /// a table of its own. Neither is composed by hand from the provider's name — the name is asked
    /// for what it can answer, which leaves any fourth provider on the branch most likely to work.
    /// </para>
    /// <para>
    /// Counted rather than asked as a yes or no, because only PostgreSQL has a boolean to answer
    /// with, and the count is cast because <c>COUNT</c> is not the same width on all three. Matched
    /// on the name alone: the schema it sits in is not asked about, which would need the name each
    /// provider gives its default one, and this is a database the sample seeder created and nothing
    /// else writes to.
    /// </para>
    /// </remarks>
    private static async Task<bool> HasTable(DbContext context, string table)
    {
        var found = context.Database.IsSqlite()
            ? context.Database.SqlQuery<int>(
                $"""
                 SELECT CAST(COUNT(*) AS int) AS "Value" FROM sqlite_master
                 WHERE type = 'table' AND name = {table}
                 """)
            : context.Database.SqlQuery<int>(
                $"""
                 SELECT CAST(COUNT(*) AS int) AS "Value" FROM INFORMATION_SCHEMA.TABLES
                 WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = {table}
                 """);

        return await found.SingleAsync() > 0;
    }
}
