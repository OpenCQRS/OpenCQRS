using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Data;

/// <summary>
/// The DCB store's four tables, which deriving from <see cref="DcbDbContext"/> brings with it.
/// A separate context from <see cref="StreamedStoreDbContext"/> because the two stores have
/// separate base classes; they share nothing and neither reads the other's tables.
/// </summary>
public sealed class DcbStoreDbContext(
    DbContextOptions<DcbDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : DcbDbContext(options, timeProvider, httpContextAccessor)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        SqliteDates.Apply(this, modelBuilder);
    }
}
