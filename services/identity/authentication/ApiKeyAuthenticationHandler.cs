using System.Security.Claims;
using System.Text.Encodings.Web;
using DMR.Services.Identity.Dtos;
using DMR.Services.Identity.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DMR.Services.Identity.Authentication;

/// <summary>
/// Authenticates every request by reading the API key out of the configured header (see
/// <see cref="ApiKeyAuthOptions.HeaderName"/>) and delegating validation to
/// <see cref="IIdentityService.ValidateApiKeyAsync"/>. Registered as the default (and only) authentication
/// scheme in Program.cs, with a fallback authorization policy requiring an authenticated user on every
/// endpoint that doesn't say otherwise — see services/identity/docs.
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyAuthOptions> apiKeyAuthOptions,
    IIdentityService identityService)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerName = apiKeyAuthOptions.Value.HeaderName;
        if (!Request.Headers.TryGetValue(headerName, out var headerValues))
            return AuthenticateResult.NoResult();

        var rawKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.NoResult();

        var result = await identityService.ValidateApiKeyAsync(
            new ValidateApiKeyRequestDto { RawKey = rawKey }, Context.RequestAborted);

        if (!result.IsValid)
            return AuthenticateResult.Fail("Invalid API key.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId!),
            new(ClaimTypes.Role, result.Role!)
        };
        if (result.KeyId is not null)
            claims.Add(new Claim("key_id", result.KeyId));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsync($"Missing or invalid API key. Send it in the '{apiKeyAuthOptions.Value.HeaderName}' header.");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Response.WriteAsync("This API key is not authorized for this operation.");
    }
}
