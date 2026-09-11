namespace DMR.Services.Ftp.Dtos;

/// <summary>Result of downloading a batch of files from the FTP server.</summary>
public class FtpDownloadFilesResponseDto
{
    /// <summary>True when every file in the batch downloaded successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Error message describing the first failure, when <see cref="Success"/> is false.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Per-file results, one entry per file that was attempted.</summary>
    public List<FtpDownloadFileResponseDto> Files { get; set; } = [];
}
