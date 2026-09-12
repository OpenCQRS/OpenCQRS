using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Data;

/// <summary>
/// The streamed event store's three tables, which deriving from <see cref="DomainDbContext"/>
/// brings with it.
/// </summary>
public sealed class StreamedStoreDbContext(
    DbContextOptions<DomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : DomainDbContext(options, timeProvider, httpContextAccessor)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        SqliteDates.Apply(this, modelBuilder);
    }
}
