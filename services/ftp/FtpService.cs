using System.IO.Enumeration;
using DMR.Services.Ftp.Dtos;
using DMR.Services.Ftp.Interfaces;
using FluentFTP;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DMR.Services.Ftp;

/// <inheritdoc cref="IFtpService"/>
public class FtpService : IFtpService
{
    /// <summary>Number of attempts for a single file download before giving up. Retries after the
    /// first attempt resume from the partial file instead of starting over — see services/ftp/docs.</summary>
    private const int MaxDownloadAttempts = 3;

    private readonly FtpConnectionOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FtpService> _logger;

    public FtpService(IOptions<FtpConnectionOptions> options, IHostEnvironment environment, ILogger<FtpService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<FtpListFilesResponseDto> ListFilesAsync(FtpListFilesRequestDto request, CancellationToken cancellationToken = default)
    {
        var remoteDir = ResolveRemotePath(request.RemoteDirectory);
        try
        {
            using var client = CreateClient();
            await client.Connect(cancellationToken);

            var listing = await client.GetListing(remoteDir, cancellationToken);
            return new FtpListFilesResponseDto
            {
                Success = true,
                Files = listing.Select(MapToFileInfoDto).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list FTP directory {RemoteDirectory}", remoteDir);
            return new FtpListFilesResponseDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<FtpDownloadFileResponseDto> DownloadFileAsync(FtpDownloadFileRequestDto request, CancellationToken cancellationToken = default)
    {
        var remotePath = ResolveRemotePath(request.RemotePath);
        var localPath = Path.Combine(GetLocalDownloadsDirectory(), Path.GetFileName(remotePath));
        try
        {
            Directory.CreateDirectory(GetLocalDownloadsDirectory());

            using var client = CreateClient();
            await client.Connect(cancellationToken);

            var existsMode = request.Overwrite ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;
            var status = await DownloadWithRetryAsync(client, localPath, remotePath, existsMode, cancellationToken);

            if (status == FtpStatus.Failed)
            {
                return new FtpDownloadFileResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Download failed for '{remotePath}'.",
                    LocalPath = localPath
                };
            }

            return new FtpDownloadFileResponseDto
            {
                Success = true,
                LocalPath = localPath,
                SizeBytes = File.Exists(localPath) ? new FileInfo(localPath).Length : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FTP file {RemotePath}", remotePath);
            return new FtpDownloadFileResponseDto { Success = false, ErrorMessage = ex.Message, LocalPath = localPath };
        }
    }

    public async Task<FtpDownloadFilesResponseDto> DownloadFilesAsync(FtpDownloadFilesRequestDto request, CancellationToken cancellationToken = default)
    {
        var remoteDir = ResolveRemotePath(request.RemoteDirectory);
        var localDir = GetLocalDownloadsDirectory();
        var response = new FtpDownloadFilesResponseDto();
        try
        {
            Directory.CreateDirectory(localDir);

            using var client = CreateClient();
            await client.Connect(cancellationToken);

            var entries = (await client.GetListing(remoteDir, cancellationToken))
                .Where(i => i.Type == FtpObjectType.File);

            if (!string.IsNullOrWhiteSpace(request.SearchPattern))
                entries = entries.Where(i => FileSystemName.MatchesSimpleExpression(request.SearchPattern, i.Name));

            var existsMode = request.Overwrite ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

            foreach (var entry in entries)
            {
                var localPath = Path.Combine(localDir, entry.Name);
                try
                {
                    var status = await DownloadWithRetryAsync(client, localPath, entry.FullName, existsMode, cancellationToken);
                    response.Files.Add(new FtpDownloadFileResponseDto
                    {
                        Success = status != FtpStatus.Failed,
                        ErrorMessage = status == FtpStatus.Failed ? $"Download failed for '{entry.FullName}'." : null,
                        LocalPath = localPath,
                        SizeBytes = File.Exists(localPath) ? new FileInfo(localPath).Length : 0
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download FTP file {RemotePath}", entry.FullName);
                    response.Files.Add(new FtpDownloadFileResponseDto { Success = false, ErrorMessage = ex.Message, LocalPath = localPath });
                }
            }

            response.Success = response.Files.All(f => f.Success);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FTP directory {RemoteDirectory}", remoteDir);
            response.Success = false;
            response.ErrorMessage = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Downloads a file, retrying on failure. The first attempt uses <paramref name="existsMode"/> as
    /// requested by the caller; any further attempts switch to <see cref="FtpLocalExists.Resume"/> so an
    /// interrupted multi-GB transfer continues from where it left off instead of restarting from byte 0.
    /// See services/ftp/docs for why this matters for this server's large exports.
    /// </summary>
    private async Task<FtpStatus> DownloadWithRetryAsync(AsyncFtpClient client, string localPath, string remotePath, FtpLocalExists existsMode, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
        {
            try
            {
                return await client.DownloadFile(localPath, remotePath, existsMode, FtpVerify.Retry, token: cancellationToken);
            }
            catch (Exception ex) when (attempt < MaxDownloadAttempts)
            {
                _logger.LogWarning(ex, "FTP download attempt {Attempt}/{MaxAttempts} failed for {RemotePath}, retrying with resume",
                    attempt, MaxDownloadAttempts, remotePath);
                existsMode = FtpLocalExists.Resume;
            }
        }

        // Unreachable: the loop always returns or throws on its last iteration.
        throw new InvalidOperationException("Download retry loop exited without a result.");
    }

    private AsyncFtpClient CreateClient()
    {
        var client = new AsyncFtpClient(_options.Host, _options.Username, _options.Password, _options.Port);
        if (_options.UseEncryption)
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;

        // Tuned for multi-GB, multi-hour downloads — see "Long-running downloads" in services/ftp/docs.
        client.Config.ConnectTimeout = 30_000;
        client.Config.DataConnectionConnectTimeout = 30_000;
        // Inactivity timeout on the data socket, not a total-transfer-time limit — it resets on every
        // chunk received, so a steady multi-hour transfer is fine. Raised from FluentFTP's 15s default
        // to tolerate brief network stalls without failing the whole download.
        client.Config.DataConnectionReadTimeout = 120_000;
        client.Config.SocketKeepAlive = true;
        client.Config.RetryAttempts = MaxDownloadAttempts;

        return client;
    }

    /// <summary>Fixed local directory every download lands in — always ".\app_files\downloads"
    /// relative to the app's content root. Not configurable per request; see services/ftp/docs for why.</summary>
    private string GetLocalDownloadsDirectory() =>
        Path.Combine(_environment.ContentRootPath, "app_files", "downloads");

    /// <summary>Resolves a remote path against <see cref="FtpConnectionOptions.RemoteBasePath"/>
    /// unless it is already absolute (starts with "/").</summary>
    private string ResolveRemotePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _options.RemoteBasePath;
        if (path.StartsWith('/'))
            return path;

        var basePath = _options.RemoteBasePath.TrimEnd('/');
        return $"{basePath}/{path}";
    }

    private static FtpFileInfoDto MapToFileInfoDto(FtpListItem item) => new()
    {
        Name = item.Name,
        FullPath = item.FullName,
        IsDirectory = item.Type == FtpObjectType.Directory,
        SizeBytes = item.Size,
        ModifiedUtc = item.Modified == DateTime.MinValue ? null : item.Modified.ToUniversalTime()
    };
}
