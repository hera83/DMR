namespace DMR.Services.Ftp.Dtos;

/// <summary>Request to download a single file from the FTP server. Files always land under the
/// service's fixed local downloads directory (see <c>FtpService.LocalDownloadsDirectory</c>) —
/// keeping the local path out of the request so callers cannot write outside it.</summary>
public class FtpDownloadFileRequestDto
{
    /// <summary>Remote path of the file to download. Relative to
    /// <see cref="FtpConnectionOptions.RemoteBasePath"/> unless it starts with "/". The local file
    /// name is taken from this path's file name.</summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>When true, overwrite the local file if it already exists. Defaults to true.</summary>
    public bool Overwrite { get; set; } = true;
}
