namespace DMR.Services.Ftp.Dtos;

/// <summary>Result of downloading a single file from the FTP server.</summary>
public class FtpDownloadFileResponseDto
{
    /// <summary>True when the download succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Error message when <see cref="Success"/> is false.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Local path the file was saved to, under the fixed downloads directory.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>Size of the downloaded file in bytes.</summary>
    public long SizeBytes { get; set; }
}
