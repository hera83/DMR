namespace DMR.Services.Identity.Dtos;

/// <summary>
/// Request for <c>IIdentityService.ValidateApiKeyAsync</c> — an internal service call made by
/// <c>ApiKeyAuthenticationHandler</c> on every incoming request, not exposed on any controller.
/// </summary>
public class ValidateApiKeyRequestDto
{
    /// <summary>The raw key value taken from the request header, exactly as the caller sent it.</summary>
    public string RawKey { get; set; } = string.Empty;
}
