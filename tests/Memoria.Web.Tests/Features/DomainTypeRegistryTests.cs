using FluentAssertions;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The registry rebuilds binding maps the whole process shares, so these run one at a time.
/// </summary>
[Collection(nameof(TypeBindingsCollection))]
public class DomainTypeRegistryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"memoria-web-{Guid.NewGuid():N}");

    private ExtensionStore Store() => new(_root);

    private DomainTypeRegistry Registry() => new(Store());

    private string PutInLibrary(string fileName)
    {
        var store = Store();
        Directory.CreateDirectory(store.LibraryDirectory);
        var path = Path.Combine(store.LibraryDirectory, fileName);
        File.WriteAllBytes(path, File.ReadAllBytes(typeof(SampleAggregate).Assembly.Location));
        return path;
    }

    [Fact]
    public void Publishes_an_empty_catalogue_before_the_first_reload()
    {
        Registry().Current.Count.Should().Be(0);
    }

    [Fact]
    public void Publishes_what_it_found()
    {
        PutInLibrary("Domain.dll");
        var registry = Registry();

        registry.Reload();

        registry.Current.StreamedAggregates.Should().Contain(type => type.Name == nameof(SampleAggregate));
        registry.Current.DcbAggregateIds.Should().Contain(type => type.Name == nameof(SampleDcbAggregateId));
    }

    [Fact]
    public void Binds_the_types_to_both_models()
    {
        PutInLibrary("Domain.dll");

        Registry().Reload();

        TypeBindings.EventTypeBindings.Should().ContainKey("SampleHappened:1");
        TypeBindings.AggregateTypeBindings.Should().ContainKey("SampleAggregate:1");
        TypeBindings.ProjectionTypeBindings.Should().ContainKey("SampleProjection:1");
        DcbTypeBindings.AggregateTypeBindings.Should().ContainKey("SampleDcbAggregate:1");
        DcbTypeBindings.ProjectionTypeBindings.Should().ContainKey("SampleDcbProjection:1");
    }

    /// <summary>
    /// Registering from scratch: what an earlier reload bound has to go, or a type the user removed
    /// would go on being offered.
    /// </summary>
    [Fact]
    public void Forgets_types_that_are_no_longer_there()
    {
        var path = PutInLibrary("Domain.dll");
        var registry = Registry();
        registry.Reload();

        File.Delete(path);
        registry.Reload();

        TypeBindings.EventTypeBindings.Should().NotContainKey("SampleHappened:1");
        registry.Current.Count.Should().Be(0);
    }

    [Fact]
    public void Picks_up_a_type_added_since_the_last_reload()
    {
        var registry = Registry();
        registry.Reload();

        PutInLibrary("Domain.dll");
        registry.Reload();

        TypeBindings.EventTypeBindings.Should().ContainKey("SampleHappened:1");
    }

    [Fact]
    public void Records_when_it_last_reloaded()
    {
        var registry = Registry();

        registry.Reload();

        registry.Current.ReloadedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Two uploads claiming one name and version cannot both be bound. Reporting beats throwing:
    /// the reload runs inside a request someone is waiting on, and everyone shares the result.
    /// </summary>
    [Fact]
    public void Reports_a_name_claimed_twice_rather_than_throwing()
    {
        PutInLibrary("Domain.dll");
        PutInLibrary("DomainAgain.dll");
        var registry = Registry();

        var reload = () => registry.Reload();

        reload.Should().NotThrow();
        registry.Current.Errors.Should().Contain(error => error.Contains("SampleHappened"));
    }

    public void Dispose()
    {
        TypeBindings.EventTypeBindings = new Dictionary<string, Type>();
        TypeBindings.AggregateTypeBindings = new Dictionary<string, Type>();
        TypeBindings.ProjectionTypeBindings = new Dictionary<string, Type>();
        DcbTypeBindings.AggregateTypeBindings = new Dictionary<string, Type>();
        DcbTypeBindings.ProjectionTypeBindings = new Dictionary<string, Type>();

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

[CollectionDefinition(nameof(TypeBindingsCollection), DisableParallelization = true)]
public class TypeBindingsCollection;
