using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Memoria.Web.Data;

/// <summary>
/// Makes the dates in a SQLite store sortable, so the pages that order by one can be drawn.
/// </summary>
/// <remarks>
/// <para>
/// SQLite has no date type. Entity Framework Core keeps a <c>DateTimeOffset</c> in it as text and
/// then refuses to order by one — <c>SQLite does not support expressions of type 'DateTimeOffset'
/// in ORDER BY clauses</c> — which is every list page this tool has, because each of them orders by
/// when something was written. Without this, all of them come back empty against a SQLite store.
/// </para>
/// <para>
/// Telling the model the column is text is enough: SQLite will then sort it, and the text it holds
/// sorts into the same order as the instants it stands for. That is true because of how the store
/// writes them and not by luck — <c>AuditInterceptor</c> stamps <c>utcNow</c>, so every value
/// carries the same <c>+00:00</c> offset, and the rest of the format is fixed-width down to the
/// fractional second. Two values with different offsets would sort by their written form rather
/// than their instant; a store whose dates were not all UTC would need more than this.
/// </para>
/// <para>
/// This belongs to the tool rather than to the store it reads. Nothing here changes what is on
/// disk: the format written is the one Entity Framework Core already writes, and the format read is
/// parsed leniently, so a store created by the packages reads back unchanged. The tool never writes
/// to the streamed store at all.
/// </para>
/// </remarks>
public static class SqliteDates
{
    /// <summary>The form Entity Framework Core keeps a <c>DateTimeOffset</c> in on SQLite.</summary>
    private const string Format = "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz";

    private static readonly ValueConverter<DateTimeOffset, string> ToText = new(
        date => date.ToString(Format, CultureInfo.InvariantCulture),
        text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None));

    private static readonly ValueConverter<DateTimeOffset?, string?> ToTextOrNothing = new(
        date => date == null ? null : date.Value.ToString(Format, CultureInfo.InvariantCulture),
        text => text == null
            ? null
            : DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None));

    /// <summary>
    /// Applies the conversion to every date in the model, on SQLite and nowhere else.
    /// </summary>
    /// <param name="context">The context being built, asked which provider it is opening.</param>
    /// <param name="modelBuilder">The model being built.</param>
    /// <remarks>
    /// Every date rather than a named few: the pages order by four of them across two stores, and a
    /// column left behind would fail only on the page that happens to sort by it.
    /// </remarks>
    public static void Apply(DbContext context, ModelBuilder modelBuilder)
    {
        if (!context.Database.IsSqlite())
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(ToText);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(ToTextOrNothing);
                }
            }
        }
    }
}
