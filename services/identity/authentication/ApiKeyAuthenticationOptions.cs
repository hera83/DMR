using Microsoft.AspNetCore.Authentication;

namespace DMR.Services.Identity.Authentication;

/// <summary>
/// Marker options type for the "ApiKey" authentication scheme — carries no settings of its own (the
/// header name and master key live in <c>ApiKeyAuthOptions</c>, bound from configuration and injected into
/// <see cref="ApiKeyAuthenticationHandler"/> directly) but is required by <c>AddScheme</c>. Lives next to
/// the handler under a dedicated authentication/ subfolder rather than dtos/ or the service root, since
/// it's ASP.NET Core auth-pipeline plumbing, not a DTO or the service implementation — see
/// services/identity/docs.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Name this scheme is registered and referenced under.</summary>
    public const string SchemeName = "ApiKey";
}
