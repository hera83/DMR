namespace DMR.Services.Identity.Dtos;

/// <summary>
/// Metadata for one issued API key, with no secret material — the raw key is only ever returned once, in
/// <see cref="CreateApiKeyResponseDto"/>, and the hash is never exposed. Exception to the request/response
/// pairing rule: a supporting item type nested inside <see cref="ListApiKeysResponseDto"/>, never sent on
/// its own.
/// </summary>
public class ApiKeyDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>First few characters of the raw key, for recognizing it in a listing.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>Null if the key still holds its originally issued secret; set to the most recent rollover
    /// time otherwise — see <c>IIdentityService.RolloverApiKeyAsync</c>.</summary>
    public DateTime? RolledOverAtUtc { get; set; }

    /// <summary>True when the key is neither revoked nor past its expiry.</summary>
    public bool IsActive { get; set; }
}
