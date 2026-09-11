# Ftp service

Downloads and lists files from an FTP/FTPS server. Built for pulling data exports (e.g. Motorstyrelsen
data drops, see `app_files/raws/`) down to local disk so the rest of the app can process them.

## Library

Uses [FluentFTP](https://github.com/robinrodricks/FluentFTP) (`FluentFTP.AsyncFtpClient`) — free, MIT-licensed,
actively maintained, no license risk. Installed via `dotnet add package FluentFTP` (see `DMR.csproj`).
Avoid `System.Net.FtpWebRequest` — it's obsolete in .NET.

## Files

- `interfaces/IFtpService.cs` — the one interface for this service.
- `FtpService.cs` — the one implementation (lives at the service folder root, next to `docs/`, `dtos/`,
  `interfaces/`, not inside a subfolder).
- `dtos/FtpConnectionOptions.cs` — connection settings, bound from the `"Ftp"` config section. Exception to
  the request/response pairing rule: it's a configuration model (read via `IOptions<FtpConnectionOptions>`),
  never something exchanged on a call.
- `dtos/FtpFileInfoDto.cs` — a single remote file/directory entry. Also an exception — a supporting item
  type nested inside `FtpListFilesResponseDto`, never sent on its own.
- `dtos/FtpListFilesRequestDto.cs` / `dtos/FtpListFilesResponseDto.cs` — for `ListFilesAsync`.
- `dtos/FtpDownloadFileRequestDto.cs` / `dtos/FtpDownloadFileResponseDto.cs` — for `DownloadFileAsync`.
- `dtos/FtpDownloadFilesRequestDto.cs` / `dtos/FtpDownloadFilesResponseDto.cs` — for `DownloadFilesAsync`
  (batch download of a whole remote directory, optionally filtered by a wildcard pattern such as `"*.zip"`).

Every request/response pair is split one-class-per-file (never combined) — this is a project-wide rule,
see "Service structure conventions" in the repo root `CLAUDE.md`.

## Local downloads directory is fixed, not a request field

Every download always lands under `.\app_files\downloads` (resolved as
`IHostEnvironment.ContentRootPath` + `app_files/downloads` in `FtpService.GetLocalDownloadsDirectory()`,
so it's correct regardless of the process's current working directory). This is deliberately **not** a
property on `FtpDownloadFileRequestDto`/`FtpDownloadFilesRequestDto` — callers can't redirect a download
to an arbitrary local path. The local file name for a single-file download is taken from the remote file's
name (`Path.GetFileName(remotePath)`).

## Configuration

Set the `"Ftp"` section (see `appsettings.json` for the placeholder shape). **Never commit real
credentials to `appsettings.json`** — put `Host`/`Username`/`Password` in `appsettings.Development.json`
(gitignored once a `.gitignore` exists), user secrets, or environment variables instead.

```json
"Ftp": {
  "Host": "ftp.example.com",
  "Port": 21,
  "Username": "user",
  "Password": "secret",
  "UseEncryption": false,
  "RemoteBasePath": "/"
}
```

`RemoteBasePath` is the root that relative remote paths in requests are resolved against; pass an
absolute path (starting with `/`) in a request to bypass it.

## Registration

Wired up in `Program.cs`:

```csharp
builder.Services.Configure<FtpConnectionOptions>(builder.Configuration.GetSection("Ftp"));
builder.Services.AddScoped<IFtpService, DMR.Services.Ftp.FtpService>();
```

## Usage

Inject `IFtpService` (namespace `DMR.Services.Ftp.Interfaces`) wherever files need to be pulled down:

```csharp
var result = await ftpService.DownloadFileAsync(new FtpDownloadFileRequestDto
{
    RemotePath = "exports/ESStatistikListeModtag-20260906-175613.zip"
});
// result.LocalPath == ".\app_files\downloads\ESStatistikListeModtag-20260906-175613.zip"
```

Each call opens a fresh `AsyncFtpClient`, connects, does the work, and disposes the connection — there's
no persistent/pooled connection to manage.

## Long-running downloads (multi-GB, hours-long transfers)

The exports this service pulls can be several GB and take an hour or two, so `FtpService.CreateClient()`
tunes FluentFTP away from its defaults, which are sized for small/quick transfers:

- **`DataConnectionReadTimeout`** raised from FluentFTP's 15s default to 120s. This is an *inactivity*
  timeout on the data socket — it resets on every chunk received, so it does **not** cap total transfer
  time. A steady multi-hour download is fine either way; the raise only adds tolerance for brief network
  stalls (a slow patch of network, a momentary drop) so they don't fail the whole transfer.
- **`SocketKeepAlive = true`** — enables TCP keepalive so a connection that's actually still alive but
  quiet isn't silently killed by a NAT/firewall/proxy sitting between us and the server.
- **`ConnectTimeout` / `DataConnectionConnectTimeout`** raised to 30s — only affects establishing the
  control/data connections, not the transfer itself.
- **Automatic resume on retry**: `DownloadWithRetryAsync` wraps every `DownloadFile` call. If a transfer
  throws (dropped connection, timeout, etc.) partway through, the next attempt (up to
  `MaxDownloadAttempts = 3`) uses `FtpLocalExists.Resume` to continue from the partial file already on
  disk instead of restarting a multi-GB download from byte 0. `FtpVerify.Retry` is also passed so FluentFTP
  itself retries when a post-download size/checksum check fails.

There is no ASP.NET request timeout involved today since nothing calls this service over HTTP yet (see
"Known gaps" below) — a plain background/service call has no such deadline. If an HTTP endpoint is ever
added to trigger a download synchronously, don't await a multi-hour transfer inside the request: kick it
off as a background task (e.g. `IHostedService`/queued background work) and let the endpoint return
immediately, otherwise Kestrel/reverse-proxy/client timeouts will cut off the response long before the
file finishes.

## Known gaps / follow-ups

- No FTP controller/endpoint yet — this is a service only, wired for internal use (e.g. from a background
  job that ingests Motorstyrelsen exports). Add a controller under `controllers/` if an HTTP-triggered
  download is ever needed — see the note above about not awaiting a multi-hour transfer inside a request.
- Certificate validation for FTPS uses FluentFTP's defaults; tighten `client.Config` (e.g.
  `ValidateCertificate` handler) before pointing this at a server with a self-signed/expired cert.
