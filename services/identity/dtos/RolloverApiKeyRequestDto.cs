namespace DMR.Services.Identity.Dtos;

/// <summary>Request for <c>IIdentityService.RolloverApiKeyAsync</c>.</summary>
public class RolloverApiKeyRequestDto
{
    public string KeyId { get; set; } = string.Empty;
}
