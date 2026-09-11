namespace DMR.Services.Identity.Dtos;

/// <summary>Request for <c>IIdentityService.CreateApiKeyAsync</c>.</summary>
public class CreateApiKeyRequestDto
{
    /// <summary>Caller-supplied label for who/what this key is for (e.g. "n8n integration"). Shown back
    /// in listings; does not need to be unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text contact info for whoever owns this key (email, phone, Slack handle, ...) — who
    /// to reach if the key needs to be revoked/rotated or is misbehaving. Optional, but recommended.</summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>Optional expiry for the new key. Leave null for a key that never expires (until revoked).</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
