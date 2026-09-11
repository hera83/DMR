namespace DMR.Services.Identity.Dtos;

/// <summary>Request for <c>IIdentityService.RevokeApiKeyAsync</c>.</summary>
public class RevokeApiKeyRequestDto
{
    public string KeyId { get; set; } = string.Empty;
}
