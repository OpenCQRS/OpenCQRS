using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class ExtensionLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"memoria-web-{Guid.NewGuid():N}");

    private ExtensionStore Store() => new(_root);

    private string PutInLibrary(string fileName, byte[] content)
    {
        var store = Store();
        Directory.CreateDirectory(store.LibraryDirectory);
        var path = Path.Combine(store.LibraryDirectory, fileName);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static byte[] SampleAssembly => File.ReadAllBytes(typeof(SampleAggregate).Assembly.Location);

    private static byte[] AnotherAssembly => File.ReadAllBytes(typeof(FluentActions).Assembly.Location);

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
        PutInLibrary("Sound.dll", SampleAssembly);

        var loaded = ExtensionLoader.Load(Store());

        loaded.Assemblies.Should().ContainSingle();
        loaded.Errors.Should().ContainSingle().Which.Should().Contain("Broken.dll");
    }

    /// <summary>
    /// Without a restart the same file is loaded again and again, so loading must not hold it open:
    /// the next upload has to be able to write over it.
    /// </summary>
    [Fact]
    public void Leaves_the_assembly_file_writable()
    {
        var path = PutInLibrary("Sound.dll", SampleAssembly);
        ExtensionLoader.Load(Store());

        var overwrite = () => File.WriteAllBytes(path, AnotherAssembly);

        overwrite.Should().NotThrow();
    }

    /// <summary>
    /// The point of reloading: an upload that replaces an assembly must be the one that takes
    /// effect, not the copy loaded the first time round.
    /// </summary>
    [Fact]
    public void Reads_an_assembly_that_was_replaced_since_the_last_load()
    {
        var path = PutInLibrary("Replaced.dll", SampleAssembly);
        var first = ExtensionLoader.Load(Store()).Assemblies.Single().GetName().Name;

        File.WriteAllBytes(path, AnotherAssembly);
        var second = ExtensionLoader.Load(Store()).Assemblies.Single().GetName().Name;

        first.Should().Be("Memoria.Web.Tests");
        second.Should().Be("FluentAssertions");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
