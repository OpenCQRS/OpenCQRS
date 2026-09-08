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
/// The check is a PostgreSQL one, which is as portable as this project needs to be — it is wired to
/// Npgsql and nothing else. The published alternative is to run the install scripts under
/// <c>scripts/install</c> by hand; see <c>docs/guides/install-the-store-schema.md</c>.
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

    private static async Task<bool> HasTable(DbContext context, string table)
    {
        var qualified = $"public.\"{table}\"";

        return await context.Database
            .SqlQuery<bool>($"SELECT to_regclass({qualified}) IS NOT NULL AS \"Value\"")
            .SingleAsync();
    }
}
