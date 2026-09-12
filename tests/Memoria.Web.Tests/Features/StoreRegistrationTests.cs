using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Store.Cosmos;
using Memoria.EventSourcing.Store.EntityFrameworkCore;
using Memoria.Web.Data;
using Memoria.Web.Extensibility;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// What the tool wires up once it knows which engine its store is in.
/// </summary>
/// <remarks>
/// The two shapes are not variations of one another. A relational store is four contexts and an
/// Entity Framework Core reader; a Cosmos store is a client and a reader that queries documents,
/// with no context at all — its model would not even build on that provider, and there is no
/// dynamic consistency boundary in it to read.
/// <para>
/// Asserted against the registrations rather than by starting the application, because what is in
/// question is which services exist. Nothing here connects to anything.
/// </para>
/// </remarks>
public class StoreRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting =>
                new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();

    private static IServiceCollection Registered(string connectionString, IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();

        services.AddStore(DatabaseConnection.Of(connectionString, configured: null),
            configuration ?? Configuration());

        return services;
    }

    [Fact]
    public void Reads_a_relational_store_through_entity_framework_core()
    {
        var services = Registered("Host=localhost;Database=memoria;Username=postgres;Password=x");

        using var scope = new AssertionScope();

        services.Should().ContainSingle(service => service.ServiceType == typeof(IStreamedReads))
            .Which.ImplementationType.Should().Be<EfStreamedReads>();

        services.Should().Contain(service => service.ServiceType == typeof(StreamedStoreDbContext),
            "the relational reader answers from a context");
        services.Should().Contain(service => service.ServiceType == typeof(DcbStoreDbContext),
            "a relational store carries the dynamic consistency boundary tables too");
    }

    [Fact]
    public void Reads_a_cosmos_store_through_its_own_client_and_no_context()
    {
        var services = Registered("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5");

        using var scope = new AssertionScope();

        services.Should().ContainSingle(service => service.ServiceType == typeof(IStreamedReads))
            .Which.ImplementationFactory.Should().NotBeNull("the reader is built from the client");

        services.Should().NotContain(service => service.ServiceType == typeof(StreamedStoreDbContext),
            "the streamed model does not build on the Cosmos provider");
        services.Should().NotContain(service => service.ServiceType == typeof(DcbStoreDbContext),
            "there is no dynamic consistency boundary store for Cosmos");
    }

    /// <summary>
    /// Refreshing a Cosmos snapshot is a write, carried by the SDK-based <c>IDomainService</c>. The
    /// branch wires that write path onto the very client the reads use, rather than opening a
    /// second one or provisioning anything, so exactly one client is registered and neither the
    /// options nor the setup <c>AddMemoriaCosmos</c> would add is present.
    /// </summary>
    [Fact]
    public void Writes_a_cosmos_store_through_the_same_client_it_reads_with()
    {
        var services = Registered("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5");

        using var scope = new AssertionScope();

        services.Should().ContainSingle(service => service.ServiceType == typeof(IDomainService))
            .Which.ImplementationType.Should().Be<CosmosDomainService>(
                "the update tab sends the write through the Cosmos domain service");
        services.Should().ContainSingle(service => service.ServiceType == typeof(ICosmosDataStore))
            .Which.ImplementationType.Should().Be<CosmosDataStore>();
        services.Should().Contain(service => service.ServiceType == typeof(CosmosClientProvider),
            "the write path reaches the container through a provider");

        services.Should().ContainSingle(service => service.ServiceType == typeof(CosmosClient),
            "the write path reuses the one client the reads already registered");
        services.Should().NotContain(service => service.ServiceType == typeof(CosmosSetup),
            "nothing is provisioned; the store already exists");
    }

    /// <summary>
    /// What the tool can offer depends on the store it was pointed at. Only the dynamic consistency
    /// boundary differs today: there is no DCB store for Cosmos, so those pages have nothing to read
    /// and are not offered rather than offered and broken.
    /// </summary>
    [Theory]
    [InlineData("Host=localhost;Database=memoria;Username=postgres;Password=x", true)]
    [InlineData("Server=.;Database=memoria;Trusted_Connection=True", true)]
    [InlineData("Data Source=memoria.db", true)]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", false)]
    public void Offers_the_dynamic_consistency_boundary_only_where_there_is_a_store_for_it(
        string connectionString, bool expected)
    {
        var services = Registered(connectionString);

        var capabilities = services
            .Single(service => service.ServiceType == typeof(StoreCapabilities))
            .ImplementationInstance
            .Should().BeOfType<StoreCapabilities>().Subject;

        capabilities.HasDcb.Should().Be(expected);
    }

    /// <summary>
    /// Refreshing a snapshot is a write, and the streamed pages send it through
    /// <c>IDomainService</c>. Every store registers one now — the relational stores through Entity
    /// Framework Core and Cosmos through its SDK-based domain service — so the update tab is offered
    /// for all of them.
    /// </summary>
    [Theory]
    [InlineData("Host=localhost;Database=memoria;Username=postgres;Password=x", true)]
    [InlineData("Server=.;Database=memoria;Trusted_Connection=True", true)]
    [InlineData("Data Source=memoria.db", true)]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", true)]
    public void Offers_the_update_only_where_a_store_can_be_written_to(
        string connectionString, bool expected)
    {
        var services = Registered(connectionString);

        var capabilities = services
            .Single(service => service.ServiceType == typeof(StoreCapabilities))
            .ImplementationInstance
            .Should().BeOfType<StoreCapabilities>().Subject;

        capabilities.CanUpdate.Should().Be(expected);
    }

    /// <summary>
    /// A Cosmos connection string names an account, not a database or a container, so those two are
    /// configuration. They default to what the store's own options default to, so a store installed
    /// with those defaults needs no settings at all.
    /// </summary>
    [Fact]
    public void Defaults_the_cosmos_database_and_container_to_the_store_s_own_defaults()
    {
        var connection = DatabaseConnection.Of(
            "AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", configured: null);

        var store = CosmosStore.Of(connection, Configuration());

        using var scope = new AssertionScope();

        store.DatabaseName.Should().Be("Memoria");
        store.ContainerName.Should().Be("Domain");
    }

    [Fact]
    public void Takes_the_cosmos_database_and_container_it_was_configured_with()
    {
        var connection = DatabaseConnection.Of(
            "AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", configured: null);

        var store = CosmosStore.Of(connection, Configuration(
            ("Database:Cosmos:DatabaseName", "orders"),
            ("Database:Cosmos:ContainerName", "events")));

        using var scope = new AssertionScope();

        store.DatabaseName.Should().Be("orders");
        store.ContainerName.Should().Be("events");
    }
}
