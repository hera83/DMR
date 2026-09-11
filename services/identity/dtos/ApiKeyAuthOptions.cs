namespace DMR.Services.Identity.Dtos;

/// <summary>
/// Settings for API-key authentication, bound from the "Identity" configuration section
/// (appsettings.json / appsettings.Development.json / user secrets). Exception to the
/// request/response pairing rule: this is a configuration model, never exchanged over a call —
/// it is only ever read via <c>IOptions&lt;ApiKeyAuthOptions&gt;</c>.
/// </summary>
public class ApiKeyAuthOptions
{
    /// <summary>
    /// The one key that authenticates as the built-in Admin role and is therefore the only credential
    /// that can call <c>KeyController</c> to create/list/revoke API keys. Leave empty in
    /// appsettings.json — put the real value in appsettings.Development.json (gitignored once a
    /// .gitignore exists), user secrets, or an environment variable. An empty value disables master-key
    /// login entirely (no request can ever match an empty string).
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>HTTP request header that carries the API key. Defaults to "X-Api-Key".</summary>
    public string HeaderName { get; set; } = "X-Api-Key";
}
