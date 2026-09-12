using System.IO.Compression;
using FluentAssertions;
using Memoria.Web.Extensibility;
using Xunit;

namespace Memoria.Web.Tests.Features;

public class ExtensionStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"memoria-web-{Guid.NewGuid():N}");

    private ExtensionStore Store() => new(_root);

    /// <summary>
    /// A zip holding the named entries, each carrying its own name as its bytes so a test can tell
    /// one file's content from another's.
    /// </summary>
    private static Stream ZipOf(params string[] entryNames)
    {
        var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entryName in entryNames)
            {
                using var entry = archive.CreateEntry(entryName).Open();
                using var writer = new StreamWriter(entry);
                writer.Write(entryName);
            }
        }

        buffer.Position = 0;
        return buffer;
    }

    [Fact]
    public void Extracts_assemblies_from_the_uploaded_archive()
    {
        Store().Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        File.Exists(Path.Combine(_root, "lib", "Contoso.Domain.dll")).Should().BeTrue();
    }

    [Fact]
    public void Keeps_the_uploaded_archive()
    {
        Store().Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        File.Exists(Path.Combine(_root, "zips", "pack.zip")).Should().BeTrue();
    }

    [Fact]
    public void Ignores_entries_that_are_not_assemblies()
    {
        Store().Install("pack.zip", ZipOf("Contoso.Domain.dll", "readme.txt", "Contoso.Domain.xml"));

        Directory.GetFiles(Path.Combine(_root, "lib")).Select(Path.GetFileName)
            .Should().BeEquivalentTo("Contoso.Domain.dll");
    }

    [Fact]
    public void Flattens_assemblies_held_in_folders()
    {
        Store().Install("pack.zip", ZipOf("bin/Release/Contoso.Domain.dll"));

        File.Exists(Path.Combine(_root, "lib", "Contoso.Domain.dll")).Should().BeTrue();
    }

    [Fact]
    public void Overwrites_an_assembly_a_previous_upload_left_behind()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        store.Install("other.zip", ZipOf("bin/Contoso.Domain.dll"));

        File.ReadAllText(Path.Combine(_root, "lib", "Contoso.Domain.dll"))
            .Should().Be("bin/Contoso.Domain.dll");
    }

    [Fact]
    public void Overwrites_an_archive_of_the_same_name()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("First.dll"));

        store.Install("pack.zip", ZipOf("Second.dll"));

        Directory.GetFiles(Path.Combine(_root, "zips")).Select(Path.GetFileName)
            .Should().BeEquivalentTo("pack.zip");
    }

    [Fact]
    public void Writes_nothing_outside_the_library_directory()
    {
        Store().Install("pack.zip", ZipOf("../../escaped.dll"));

        File.Exists(Path.Combine(_root, "lib", "escaped.dll")).Should().BeTrue();
        File.Exists(Path.Combine(_root, "..", "escaped.dll")).Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_upload_that_is_not_a_zip_archive()
    {
        var notAZip = new MemoryStream("plain text"u8.ToArray());

        var install = () => Store().Install("pack.zip", notAZip);

        install.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Leaves_no_archive_behind_when_the_upload_is_not_a_zip()
    {
        var notAZip = new MemoryStream("plain text"u8.ToArray());

        try
        {
            Store().Install("pack.zip", notAZip);
        }
        catch (InvalidDataException)
        {
            // The point of the test is what is on disk afterwards.
        }

        File.Exists(Path.Combine(_root, "zips", "pack.zip")).Should().BeFalse();
    }

    /// <summary>
    /// Removing an archive has to take its assemblies with it, or the types it brought would go on
    /// being registered from a library nothing accounts for.
    /// </summary>
    [Fact]
    public void Removing_an_archive_removes_the_assemblies_it_brought()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        store.Remove("pack.zip");

        store.AssemblyPaths().Should().BeEmpty();
    }

    [Fact]
    public void Removing_an_archive_leaves_the_others_alone()
    {
        var store = Store();
        store.Install("one.zip", ZipOf("One.dll"));
        store.Install("two.zip", ZipOf("Two.dll"));

        store.Remove("one.zip");

        store.AssemblyPaths().Select(Path.GetFileName).Should().BeEquivalentTo("Two.dll");
        store.InstalledArchives().Select(archive => archive.Name).Should().BeEquivalentTo("two.zip");
    }

    /// <summary>
    /// Two archives can carry an assembly of the same name, and the one still installed keeps it.
    /// </summary>
    [Fact]
    public void Keeps_an_assembly_another_archive_also_carries()
    {
        var store = Store();
        store.Install("one.zip", ZipOf("Shared.dll"));
        store.Install("two.zip", ZipOf("Shared.dll"));

        store.Remove("one.zip");

        store.AssemblyPaths().Select(Path.GetFileName).Should().BeEquivalentTo("Shared.dll");
    }

    [Fact]
    public void Removing_something_that_is_not_installed_does_nothing()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        var remove = () => store.Remove("never-installed.zip");

        remove.Should().NotThrow();
        store.InstalledArchives().Should().ContainSingle();
    }

    /// <summary>
    /// The name arrives from a form, where a path could be typed instead.
    /// </summary>
    [Fact]
    public void Refuses_to_remove_anything_outside_the_store()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("Contoso.Domain.dll"));

        store.Remove("../../pack.zip");

        store.InstalledArchives().Should().ContainSingle();
    }

    [Fact]
    public void Lists_the_installed_archives()
    {
        var store = Store();
        store.Install("one.zip", ZipOf("One.dll"));
        store.Install("two.zip", ZipOf("Two.dll"));

        store.InstalledArchives().Select(archive => archive.Name)
            .Should().BeEquivalentTo("one.zip", "two.zip");
    }

    /// <summary>
    /// The settings page says what each archive brought, so an archive has to know which assembly
    /// files it holds — by file name alone, the same way they were extracted.
    /// </summary>
    [Fact]
    public void Lists_the_assemblies_each_archive_holds()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("bin/Release/Two.dll", "One.dll", "readme.txt"));

        store.InstalledArchives().Single().Assemblies.Should().Equal("One.dll", "Two.dll");
    }

    [Fact]
    public void Lists_no_archives_before_anything_is_uploaded()
    {
        Store().InstalledArchives().Should().BeEmpty();
    }

    [Fact]
    public void Reports_the_assemblies_to_load()
    {
        var store = Store();
        store.Install("pack.zip", ZipOf("Contoso.Domain.dll", "Contoso.Contracts.dll"));

        store.AssemblyPaths().Select(Path.GetFileName)
            .Should().BeEquivalentTo("Contoso.Domain.dll", "Contoso.Contracts.dll");
    }

    [Fact]
    public void Reports_no_assemblies_before_anything_is_uploaded()
    {
        Store().AssemblyPaths().Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
