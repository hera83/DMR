namespace DMR.Services.Identity.Dtos;

/// <summary>
/// The two Identity roles this service seeds at startup (see <c>IdentityService.EnsureSeededAsync</c>).
/// Exception to the request/response pairing rule: a small set of supporting constants, never itself
/// exchanged on a call.
/// </summary>
public static class IdentityRoleNames
{
    /// <summary>Granted only to a request authenticated with the configured master key (see
    /// <see cref="ApiKeyAuthOptions.MasterKey"/>) — never persisted against a stored <c>ApiKey</c> row.
    /// <c>KeyController</c> requires this role, so the master key is the only credential that can
    /// administer API keys.</summary>
    public const string Admin = "Admin";

    /// <summary>Granted to every request authenticated with a regular, non-revoked, non-expired
    /// <c>ApiKey</c> row.</summary>
    public const string ApiKeyHolder = "ApiKeyHolder";
}
