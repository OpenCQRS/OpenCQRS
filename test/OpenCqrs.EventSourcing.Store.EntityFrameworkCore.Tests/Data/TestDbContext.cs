using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Data.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Data;

public sealed class TestDbContext(
    DbContextOptions<DomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : DomainDbContext(options, timeProvider, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<TestItemEntity>()
            .ToTable(name: "Items");
    }

    public DbSet<TestItemEntity> Items { get; set; } = null!;
}
