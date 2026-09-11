namespace DMR.Services.Identity.Dtos;

/// <summary>Response for <c>IIdentityService.CreateApiKeyAsync</c>.</summary>
public class CreateApiKeyResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>
    /// The raw API key, e.g. <c>dmr_&lt;random&gt;</c>. Shown exactly once — only the hash is persisted,
    /// so if this is lost the key can only be revoked and re-issued, never recovered.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
