namespace DMR.Services.Ftp.Dtos;

/// <summary>Request to download every file in a remote directory (optionally filtered). Files always
/// land under the service's fixed local downloads directory (see
/// <c>FtpService.LocalDownloadsDirectory</c>) — keeping the local path out of the request so callers
/// cannot write outside it.</summary>
public class FtpDownloadFilesRequestDto
{
    /// <summary>Remote directory to download from. Relative to
    /// <see cref="FtpConnectionOptions.RemoteBasePath"/> unless it starts with "/".</summary>
    public string RemoteDirectory { get; set; } = string.Empty;

    /// <summary>Optional wildcard pattern (e.g. "*.zip") to filter which files are downloaded.
    /// Null or empty means all files.</summary>
    public string? SearchPattern { get; set; }

    /// <summary>When true, overwrite local files that already exist. Defaults to true.</summary>
    public bool Overwrite { get; set; } = true;
}
