using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Store.Cosmos;
using Memoria.Web.Data;
using Memoria.Web.Samples.Data;
using Memoria.Web.Samples.Seeding;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Memoria.Web.Samples.Tests.Features;

/// <summary>
/// What the seeder wires up once it knows which engine the store it was pointed at is in.
/// </summary>
/// <remarks>
/// The seeder reads the same connection string the web tool does, so it meets the same two shapes.
/// A relational store is written through contexts Entity Framework Core opens; a Cosmos store has no
/// context to open and is written through the SDK-based domain service instead. Registering the
/// relational half for a Cosmos store is what used to throw on start-up, which is why the two shapes
/// are asserted rather than assumed.
/// <para>
/// Asserted against the registrations rather than by running a seed, because what is in question is
/// which services exist. Nothing here connects to anything.
/// </para>
/// </remarks>
public class SampleStoreRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting =>
                new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();

    private static IServiceCollection Registered(string connectionString, IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();

        services.AddSampleStore(DatabaseConnection.Of(connectionString, configured: null),
            configuration ?? Configuration());

        return services;
    }

    [Fact]
    public void Seeds_a_relational_store_through_entity_framework_core()
    {
        var services = Registered("Host=localhost;Database=memoria_samples;Username=postgres;Password=x");

        using var scope = new AssertionScope();

        services.Should().ContainSingle(service => service.ServiceType == typeof(ISampleStore))
            .Which.ImplementationType.Should().Be<RelationalSampleStore>();

        services.Should().Contain(service => service.ServiceType == typeof(StreamedStoreDbContext),
            "the streamed store is written through a context");
        services.Should().Contain(service => service.ServiceType == typeof(DcbStoreDbContext),
            "a relational store carries the dynamic consistency boundary tables too");
    }

    /// <summary>
    /// The shape that used to throw. A Cosmos store has no <c>DbContext</c>, so asking the shared
    /// <see cref="DatabaseConnection.Apply"/> for a provider is what the seeder must not do — it
    /// registers the SDK write path instead, and no context at all.
    /// </summary>
    [Fact]
    public void Seeds_a_cosmos_store_through_its_own_client_and_no_context()
    {
        var services = Registered("AccountEndpoint=https://localhost:8081/;AccountKey=a2V5");

        using var scope = new AssertionScope();

        services.Should().ContainSingle(service => service.ServiceType == typeof(ISampleStore))
            .Which.ImplementationType.Should().Be<CosmosSampleStore>();

        services.Should().ContainSingle(service => service.ServiceType == typeof(IDomainService))
            .Which.ImplementationType.Should().Be<CosmosDomainService>(
                "the sample data is written through the Cosmos domain service");
        services.Should().ContainSingle(service => service.ServiceType == typeof(CosmosClient),
            "one client, which the store's provider is handed");

        services.Should().NotContain(service => service.ServiceType == typeof(StreamedStoreDbContext),
            "the streamed model does not build on the Cosmos provider");
        services.Should().NotContain(service => service.ServiceType == typeof(DcbStoreDbContext),
            "there is no dynamic consistency boundary store for Cosmos");
    }

    /// <summary>
    /// The database and the container are settings rather than part of the connection string, and the
    /// seeder must read them from the same two keys the tool does — otherwise it fills a container
    /// the tool does not open.
    /// </summary>
    [Fact]
    public void Reaches_the_cosmos_container_the_tool_was_configured_with()
    {
        var connection = DatabaseConnection.Of(
            "AccountEndpoint=https://localhost:8081/;AccountKey=a2V5", configured: null);

        var store = CosmosStore.Of(connection, Configuration(
            (CosmosStore.DatabaseSetting, "samples"),
            (CosmosStore.ContainerSetting, "domain-samples")));

        using var scope = new AssertionScope();

        store.DatabaseName.Should().Be("samples");
        store.ContainerName.Should().Be("domain-samples");
    }
}
