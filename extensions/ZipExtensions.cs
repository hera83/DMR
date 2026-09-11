using System.IO.Compression;

namespace DMR.Extensions;

/// <summary>
/// Static zip-archive helpers. Part of ".\extensions" — one file of static functions per helper
/// (e.g. <c>ZipExtensions</c>, <c>StringExtensions</c>), see the repo root CLAUDE.md.
/// </summary>
public static class ZipExtensions
{
    /// <summary>Fixed directory every unpack always extracts into — ".\app_files\temp" relative to the
    /// process's working directory. Not a parameter: callers can't redirect extraction elsewhere.</summary>
    private static string UnpackDirectory => Path.Combine(Directory.GetCurrentDirectory(), "app_files", "temp");

    /// <summary>
    /// Unpacks a zip archive held in memory into ".\app_files\temp" (created if missing), overwriting
    /// any file already there with the same name. Returns the full local paths of every file the
    /// archive contained (directory entries are skipped, not included in the result).
    /// </summary>
    /// <remarks>
    /// A <c>byte[]</c> can never hold more than ~2 GB (array indices are <c>Int32</c>-limited in .NET,
    /// regardless of platform or available RAM), so this overload only works for small zips. For
    /// anything that might be larger — e.g. a multi-GB export already sitting on disk — use
    /// <see cref="Unpack(string)"/> instead, which streams from the file and has no such limit.
    /// </remarks>
    public static List<string> Unpack(this byte[] file)
    {
        using var stream = new MemoryStream(file);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        return ExtractAll(archive);
    }

    /// <summary>
    /// Unpacks the zip archive at <paramref name="zipPath"/> into ".\app_files\temp" (created if
    /// missing), overwriting any file already there with the same name. Returns the full local paths
    /// of every file the archive contained (directory entries are skipped, not included in the result).
    /// </summary>
    /// <remarks>
    /// Reads the archive straight off disk via <see cref="ZipFile.OpenRead"/> instead of loading it
    /// into memory first, so — unlike <see cref="Unpack(byte[])"/> — this works regardless of file
    /// size (e.g. multi-GB Motorstyrelsen exports under app_files/downloads).
    /// </remarks>
    public static List<string> Unpack(this string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        return ExtractAll(archive);
    }

    private static List<string> ExtractAll(ZipArchive archive)
    {
        var targetDirectory = Path.GetFullPath(UnpackDirectory);
        Directory.CreateDirectory(targetDirectory);

        var extractedFiles = new List<string>();

        foreach (var entry in archive.Entries)
        {
            // Directory entries have an empty Name (their FullName ends in "/") — nothing to extract.
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));

            // Guard against "zip slip": a malicious entry name like "../../evil.dll" combined above
            // could otherwise resolve outside targetDirectory. Refuse anything that does.
            if (!destinationPath.StartsWith(targetDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Zip entry '{entry.FullName}' would extract outside of '{targetDirectory}'.");

            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
                Directory.CreateDirectory(destinationDir);

            entry.ExtractToFile(destinationPath, overwrite: true);
            extractedFiles.Add(destinationPath);
        }

        return extractedFiles;
    }
}
