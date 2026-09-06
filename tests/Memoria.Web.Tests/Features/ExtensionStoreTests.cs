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

    [Fact]
    public void Lists_the_installed_archives()
    {
        var store = Store();
        store.Install("one.zip", ZipOf("One.dll"));
        store.Install("two.zip", ZipOf("Two.dll"));

        store.InstalledArchives().Select(archive => archive.Name)
            .Should().BeEquivalentTo("one.zip", "two.zip");
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
