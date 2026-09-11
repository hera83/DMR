using DMR.Services.Ftp.Dtos;

namespace DMR.Services.Ftp.Interfaces;

/// <summary>
/// Downloads and lists files on an FTP/FTPS server configured via <see cref="FtpConnectionOptions"/>.
/// See services/ftp/docs for setup and usage notes.
/// </summary>
public interface IFtpService
{
    /// <summary>Lists the files and directories in a remote directory.</summary>
    Task<FtpListFilesResponseDto> ListFilesAsync(FtpListFilesRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Downloads a single file from the server to a local path.</summary>
    Task<FtpDownloadFileResponseDto> DownloadFileAsync(FtpDownloadFileRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Downloads every file in a remote directory (optionally filtered) to a local directory.</summary>
    Task<FtpDownloadFilesResponseDto> DownloadFilesAsync(FtpDownloadFilesRequestDto request, CancellationToken cancellationToken = default);
}
