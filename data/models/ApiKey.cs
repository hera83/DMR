namespace DMR.Data.Models;

/// <summary>
/// Persistence entity for one issued API key. Only <see cref="KeyHash"/> (a SHA-256 hash of the raw key)
/// and <see cref="KeyPrefix"/> (a short, non-secret display fragment) are ever stored — the raw key itself
/// is generated, returned to the caller once, and then discarded; see
/// <c>IdentityService.CreateApiKeyAsync</c>.
/// </summary>
public class ApiKey
{
    /// <summary>Primary key (a GUID string, assigned when the key is created).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Foreign key to the <see cref="ApplicationUser"/> that owns this key.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Navigation property to the owning user.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>Caller-supplied label identifying who/what this key is for (e.g. "n8n integration").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text contact info for whoever owns this key (email, phone, Slack handle, ...) — who
    /// to reach if the key needs to be revoked/rotated or is misbehaving.</summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>Lowercase-hex SHA-256 hash of the raw key. Looked up on every request; see
    /// <c>IdentityService.ValidateApiKeyAsync</c>.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>First few characters of the raw key, kept in the clear so an admin can recognize a key in
    /// listings without the full secret ever being persisted or re-displayed.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional expiry; a key past this instant is treated as invalid even if not revoked.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Set when an admin revokes the key; a non-null value makes it invalid regardless of expiry.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Updated on every successful authentication; null if the key has never been used.</summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>Set every time the key's secret material is rolled over (see
    /// <c>IdentityService.RolloverApiKeyAsync</c>); null if it still holds its originally issued secret.</summary>
    public DateTime? RolledOverAtUtc { get; set; }
}
