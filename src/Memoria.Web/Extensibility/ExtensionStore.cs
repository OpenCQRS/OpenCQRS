using System.IO.Compression;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Where uploaded archives and the assemblies taken out of them are kept.
/// </summary>
/// <param name="root">The directory holding both, created on first use.</param>
public sealed class ExtensionStore(string root)
{
    private const string AssemblyExtension = ".dll";

    /// <summary>Gets the directory the uploaded archives are kept in, as sent.</summary>
    public string ArchiveDirectory { get; } = Path.Combine(root, "zips");

    /// <summary>Gets the directory the assemblies are extracted to and loaded from.</summary>
    public string LibraryDirectory { get; } = Path.Combine(root, "lib");

    /// <summary>
    /// Stores one uploaded archive and extracts the assemblies in it.
    /// </summary>
    /// <param name="fileName">The name the archive was uploaded under.</param>
    /// <param name="content">The archive.</param>
    /// <exception cref="InvalidDataException">The upload is not a zip archive.</exception>
    /// <remarks>
    /// The archive is read before anything is written, so an upload that turns out not to be a zip
    /// leaves the store as it was. Assemblies are then written by file name alone: an entry named
    /// <c>bin/Release/Contoso.dll</c> and one named <c>../../Contoso.dll</c> both land on
    /// <c>lib/Contoso.dll</c>, which is what keeps a crafted archive from writing outside the store.
    /// An assembly of the same name from an earlier upload is replaced.
    /// </remarks>
    public void Install(string fileName, Stream content)
    {
        var archiveName = Path.GetFileName(fileName);

        // Buffered because the archive is read twice — once to validate, once to extract — and an
        // upload stream is not necessarily seekable.
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true))
        {
            Directory.CreateDirectory(LibraryDirectory);

            foreach (var entry in archive.Entries.Where(IsAssembly))
            {
                entry.ExtractToFile(Path.Combine(LibraryDirectory, Path.GetFileName(entry.Name)),
                    overwrite: true);
            }
        }

        Directory.CreateDirectory(ArchiveDirectory);
        buffer.Position = 0;

        using var stored = File.Create(Path.Combine(ArchiveDirectory, archiveName));
        buffer.CopyTo(stored);
    }

    /// <summary>Gets the archives uploaded so far, newest first.</summary>
    public IReadOnlyList<InstalledArchive> InstalledArchives()
    {
        if (!Directory.Exists(ArchiveDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(ArchiveDirectory, "*.zip")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => new InstalledArchive(file.Name, file.Length, file.LastWriteTimeUtc))
            .ToList();
    }

    /// <summary>Gets the assemblies to load, in a stable order.</summary>
    public IReadOnlyList<string> AssemblyPaths()
    {
        if (!Directory.Exists(LibraryDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(LibraryDirectory, $"*{AssemblyExtension}")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // A directory entry has an empty Name; only files are extracted.
    private static bool IsAssembly(ZipArchiveEntry entry) =>
        !string.IsNullOrEmpty(entry.Name) &&
        entry.Name.EndsWith(AssemblyExtension, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One archive on disk.
/// </summary>
/// <param name="Name">The file name it was uploaded under.</param>
/// <param name="Length">Its size in bytes.</param>
/// <param name="UploadedUtc">When it was last written.</param>
public sealed record InstalledArchive(string Name, long Length, DateTime UploadedUtc);
