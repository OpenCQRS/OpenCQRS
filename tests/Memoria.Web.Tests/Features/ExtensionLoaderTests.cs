using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class ExtensionLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"memoria-web-{Guid.NewGuid():N}");

    private ExtensionStore Store() => new(_root);

    private void PutInLibrary(string fileName, byte[] content)
    {
        var store = Store();
        Directory.CreateDirectory(store.LibraryDirectory);
        File.WriteAllBytes(Path.Combine(store.LibraryDirectory, fileName), content);
    }

    [Fact]
    public void Loads_nothing_when_no_assembly_has_been_uploaded()
    {
        var loaded = ExtensionLoader.Load(Store());

        loaded.Assemblies.Should().BeEmpty();
        loaded.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Reports_a_file_that_is_not_an_assembly_rather_than_throwing()
    {
        PutInLibrary("Broken.dll", "not an assembly"u8.ToArray());

        var loaded = ExtensionLoader.Load(Store());

        loaded.Assemblies.Should().BeEmpty();
        loaded.Errors.Should().ContainSingle().Which.Should().Contain("Broken.dll");
    }

    [Fact]
    public void Loads_the_assemblies_it_can_and_reports_the_ones_it_cannot()
    {
        PutInLibrary("Broken.dll", "not an assembly"u8.ToArray());
        PutInLibrary("Sound.dll", File.ReadAllBytes(typeof(SampleAggregate).Assembly.Location));

        var loaded = ExtensionLoader.Load(Store());

        loaded.Assemblies.Should().ContainSingle();
        loaded.Errors.Should().ContainSingle().Which.Should().Contain("Broken.dll");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
