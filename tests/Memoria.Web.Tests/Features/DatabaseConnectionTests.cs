using System;
using FluentAssertions;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// Which provider the tool opens its store with, read off the connection string it was given or
/// off the setting that overrides it. The tool is handed a store somebody else created, so the
/// connection string is all it has to go on until the setting says otherwise.
/// </summary>
public class DatabaseConnectionTests
{
    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=memoria;Username=postgres;Password=password")]
    [InlineData("Host=db;Database=memoria;Username=postgres;Password=password")]
    [InlineData("Server=db;Port=5432;Database=memoria;User Id=postgres;Password=password")]
    public void Reads_postgres_off_the_keywords_only_it_takes(string connectionString)
    {
        DatabaseConnection.Of(connectionString, configured: null).Provider
            .Should().Be(DatabaseProvider.Npgsql);
    }

    [Theory]
    [InlineData("Server=.;Database=memoria;Trusted_Connection=True;TrustServerCertificate=True")]
    [InlineData("Server=localhost;Initial Catalog=memoria;User Id=sa;Password=password")]
    [InlineData(@"Data Source=(localdb)\MSSQLLocalDB;Database=memoria;Integrated Security=True")]
    [InlineData("Server=tcp:memoria.database.windows.net,1433;Database=memoria;Authentication=Active Directory Default")]
    public void Reads_sql_server_off_the_keywords_only_it_takes(string connectionString)
    {
        DatabaseConnection.Of(connectionString, configured: null).Provider
            .Should().Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("Data Source=memoria.db")]
    [InlineData("Data Source=App_Data/memoria.sqlite")]
    [InlineData(@"Data Source=C:\Memoria\App_Data\memoria.db;Cache=Shared")]
    [InlineData("Filename=memoria.db3;Foreign Keys=True")]
    public void Reads_sqlite_off_a_file_it_would_open(string connectionString)
    {
        DatabaseConnection.Of(connectionString, configured: null).Provider
            .Should().Be(DatabaseProvider.Sqlite);
    }

    /// <summary>
    /// Cosmos names its account rather than a server, and no other provider takes either keyword.
    /// </summary>
    [Theory]
    [InlineData("AccountEndpoint=https://memoria.documents.azure.com:443/;AccountKey=a2V5")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5;")]
    public void Reads_cosmos_off_the_keywords_only_it_takes(string connectionString)
    {
        DatabaseConnection.Of(connectionString, configured: null).Provider
            .Should().Be(DatabaseProvider.Cosmos);
    }

    /// <summary>
    /// The setting is the way out of a string this cannot read, so it answers even when the string
    /// reads as something else — being told is not a guess to be second-guessed.
    /// </summary>
    [Theory]
    [InlineData("Cosmos", DatabaseProvider.Cosmos)]
    [InlineData("cosmos db", DatabaseProvider.Cosmos)]
    [InlineData("CosmosDb", DatabaseProvider.Cosmos)]
    [InlineData("Npgsql", DatabaseProvider.Npgsql)]
    [InlineData("postgres", DatabaseProvider.Npgsql)]
    [InlineData("PostgreSQL", DatabaseProvider.Npgsql)]
    [InlineData("SqlServer", DatabaseProvider.SqlServer)]
    [InlineData("sql server", DatabaseProvider.SqlServer)]
    [InlineData("MSSQL", DatabaseProvider.SqlServer)]
    [InlineData("SQLite", DatabaseProvider.Sqlite)]
    public void Takes_the_provider_it_was_configured_with(string configured, DatabaseProvider expected)
    {
        DatabaseConnection.Of("Host=localhost;Database=memoria;Username=postgres", configured)
            .Provider.Should().Be(expected);
    }

    [Fact]
    public void Carries_the_connection_string_it_was_given()
    {
        const string connectionString = "Host=localhost;Database=memoria;Username=postgres";

        DatabaseConnection.Of(connectionString, configured: null).ConnectionString
            .Should().Be(connectionString);
    }

    /// <summary>
    /// Every keyword in it belongs to more than one provider, so reading it would be a guess. The
    /// setting is what settles it, and the message says so rather than picking one.
    /// </summary>
    [Theory]
    [InlineData("Server=localhost;Database=memoria;User Id=sa;Password=password")]
    [InlineData(@"Data Source=.\SQLEXPRESS;Database=memoria")]
    [InlineData("Data Source=memoria")]
    public void Refuses_to_guess_at_a_string_that_could_be_more_than_one_provider(string connectionString)
    {
        var refusing = () => DatabaseConnection.Of(connectionString, configured: null);

        refusing.Should().Throw<InvalidOperationException>()
            .WithMessage("*Database:Provider*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Says_which_connection_string_is_missing(string? connectionString)
    {
        var refusing = () => DatabaseConnection.Of(connectionString, configured: null);

        refusing.Should().Throw<InvalidOperationException>().WithMessage("*Memoria*");
    }

    [Fact]
    public void Says_which_providers_it_knows_when_configured_with_one_it_does_not()
    {
        var refusing = () => DatabaseConnection.Of("Data Source=memoria.db", configured: "MySql");

        refusing.Should().Throw<InvalidOperationException>()
            .WithMessage("*MySql*").WithMessage("*Npgsql*");
    }

    /// <summary>
    /// An in-memory SQLite database lives as long as the connection that opened it, and the tool
    /// opens one per unit of work — so it would read an empty store rather than the one that was
    /// seeded. Refused where it is asked for rather than reported as a store with nothing in it.
    /// </summary>
    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=memoria;Mode=Memory;Cache=Shared")]
    public void Refuses_a_sqlite_database_that_only_lives_as_long_as_a_connection(string connectionString)
    {
        var refusing = () => DatabaseConnection.Of(connectionString, configured: null);

        refusing.Should().Throw<InvalidOperationException>().WithMessage("*memory*");
    }

    /// <summary>
    /// The provider read off the string is the one the context is actually opened with. Nothing is
    /// connected to: the name is what EF Core resolved from the options it was handed.
    /// </summary>
    [Theory]
    [InlineData("Host=localhost;Database=memoria;Username=postgres", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData("Server=.;Database=memoria;Trusted_Connection=True", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("Data Source=memoria.db", "Microsoft.EntityFrameworkCore.Sqlite")]
    public void Opens_a_context_with_the_provider_it_read(string connectionString, string expected)
    {
        var options = new DbContextOptionsBuilder<DcbDbContext>();

        DatabaseConnection.Of(connectionString, configured: null).Apply(options);

        using var context = new DcbStoreDbContext(options.Options, TimeProvider.System,
            Substitute.For<IHttpContextAccessor>());

        context.Database.ProviderName.Should().Be(expected);
    }

    /// <summary>
    /// Two different problems, and telling a reader the wrong one sends them the wrong way. A string
    /// of shared keywords needs the setting to choose between providers that could all open it; a
    /// string of keywords none of them takes is not a string this tool can open at all, and saying
    /// "more than one provider takes them" would be untrue.
    /// </summary>
    [Fact]
    public void Separates_a_string_naming_no_engine_from_one_naming_several()
    {
        var naming_none = () => DatabaseConnection.Of("Foo=bar;Baz=qux", configured: null);
        var naming_several = () => DatabaseConnection.Of(
            "Server=localhost;Database=memoria;User Id=sa;Password=password", configured: null);

        naming_none.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().NotContain("more than one provider takes");

        naming_several.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("more than one provider takes");
    }

    /// <summary>
    /// Cosmos is not reached through a context, so there is no provider to put on one. Said where it
    /// is asked for, rather than left to fail as an unhandled enum somewhere further in.
    /// </summary>
    [Fact]
    public void Refuses_to_open_a_context_on_cosmos()
    {
        var connection = DatabaseConnection.Of(
            "AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", configured: null);

        var applying = () => connection.Apply(new DbContextOptionsBuilder<DcbDbContext>());

        applying.Should().Throw<InvalidOperationException>().WithMessage("*Cosmos*");
    }

    [Fact]
    public void Says_a_connection_string_it_cannot_read_at_all_is_the_problem()
    {
        var refusing = () => DatabaseConnection.Of("this is not a connection string", configured: null);

        refusing.Should().Throw<InvalidOperationException>().WithMessage("*Memoria*");
    }
}
