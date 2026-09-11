namespace DMR.Services.Ftp.Dtos;

/// <summary>Result of listing a directory on the FTP server.</summary>
public class FtpListFilesResponseDto
{
    /// <summary>True when the listing succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Error message when <see cref="Success"/> is false.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Entries found in the directory. Empty when <see cref="Success"/> is false.</summary>
    public List<FtpFileInfoDto> Files { get; set; } = [];
}
