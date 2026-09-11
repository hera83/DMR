namespace DMR.Services.Ftp.Dtos;

/// <summary>
/// Describes a single entry (file or directory) found on the FTP server.
/// This is a supporting item type used inside <see cref="FtpListFilesResponseDto"/>, not a
/// standalone request/response pair — it never travels on its own.
/// </summary>
public class FtpFileInfoDto
{
    /// <summary>File or directory name (no path).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Full remote path to the entry.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>True if the entry is a directory rather than a file.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>File size in bytes. 0 for directories or when unknown.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Last modified timestamp reported by the server, if available.</summary>
    public DateTime? ModifiedUtc { get; set; }
}
