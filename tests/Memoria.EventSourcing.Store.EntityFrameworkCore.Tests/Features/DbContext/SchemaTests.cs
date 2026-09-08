using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Entities;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Memoria.EventSourcing.Store.EntityFrameworkCore.Tests.Features.DbContext;

/// <summary>
/// The relational model, built against SQL Server without connecting to one.
/// </summary>
/// <remarks>
/// Table names are relational metadata, so the in-memory provider cannot report them. Building the
/// model against a real provider needs no database; the container tests later verify the same names
/// against a running engine.
/// </remarks>
public class SchemaTests
{
    private static TestDbContext SqlServerContext() =>
        new(new DbContextOptionsBuilder<DomainDbContext>()
                .UseSqlServer("Server=none;Database=none;Trusted_Connection=True;")
                .Options,
            new FakeTimeProvider(),
            Shared.CreateHttpContextAccessor());

    [Fact]
    public void Every_table_the_store_owns_is_prefixed_with_the_domain_it_belongs_to()
    {
        // The three tables may share a database — and a DbContext — with tables that are none of
        // Memoria's business, so each carries a prefix saying whose it is. `events` was the one that
        // did not, and it is the name most likely to already be taken.
        using var context = SqlServerContext();

        using (new AssertionScope())
        {
            context.Model.FindEntityType(typeof(EventEntity))!.GetTableName().Should().Be("DomainEvents");
            context.Model.FindEntityType(typeof(AggregateEntity))!.GetTableName().Should().Be("DomainAggregates");
            context.Model.FindEntityType(typeof(ProjectionEntity))!.GetTableName().Should().Be("DomainProjections");
        }
    }
}
