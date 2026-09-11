namespace DMR.Services.Ftp.Dtos;

/// <summary>
/// Connection settings for the FTP service, bound from configuration (section "Ftp" in
/// appsettings.json / appsettings.Development.json / user secrets).
/// This is a configuration model, not a request/response pair, since it is never
/// exchanged over an API call — it is only ever read from <c>IOptions&lt;FtpConnectionOptions&gt;</c>.
/// </summary>
public class FtpConnectionOptions
{
    /// <summary>Hostname or IP address of the FTP server.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Port to connect on. Defaults to the standard FTP control port.</summary>
    public int Port { get; set; } = 21;

    /// <summary>Username used to authenticate.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password used to authenticate. Keep this out of appsettings.json — use
    /// appsettings.Development.json (gitignored) or user secrets / environment variables.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Whether to require FTPS (explicit TLS). Defaults to false for plain FTP.</summary>
    public bool UseEncryption { get; set; } = false;

    /// <summary>Optional base/remote root directory to resolve relative remote paths against.</summary>
    public string RemoteBasePath { get; set; } = "/";
}
