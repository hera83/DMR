namespace DMR.Services.Ftp.Dtos;

/// <summary>Request to list the contents of a directory on the FTP server.</summary>
public class FtpListFilesRequestDto
{
    /// <summary>Remote directory to list. Relative to <see cref="FtpConnectionOptions.RemoteBasePath"/>
    /// unless it starts with "/". Defaults to the base path when left empty.</summary>
    public string RemoteDirectory { get; set; } = string.Empty;
}
