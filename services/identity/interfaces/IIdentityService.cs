using DMR.Services.Identity.Dtos;

namespace DMR.Services.Identity.Interfaces;

/// <summary>
/// Issues, lists, revokes and validates API keys on top of ASP.NET Core Identity (each key is owned by an
/// auto-created <c>ApplicationUser</c> in the <c>ApiKeyHolder</c> role). The configured master key
/// authenticates separately, as a synthetic Admin identity with no stored user row — see
/// services/identity/docs.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates the <c>Admin</c>/<c>ApiKeyHolder</c> roles if they don't already exist. Safe to
    /// call on every startup.</summary>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);

    /// <summary>Generates a new random API key, persists its hash under a freshly created owning user, and
    /// returns the raw key — the only time it is ever available in full.</summary>
    Task<CreateApiKeyResponseDto> CreateApiKeyAsync(CreateApiKeyRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Lists issued API keys (metadata only — no key material).</summary>
    Task<ListApiKeysResponseDto> ListApiKeysAsync(ListApiKeysRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Revokes a key by id. Revocation is permanent; a revoked key cannot be un-revoked, only
    /// replaced with a newly created one.</summary>
    Task<RevokeApiKeyResponseDto> RevokeApiKeyAsync(RevokeApiKeyRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Generates fresh secret material for an existing, non-revoked key and returns the new raw
    /// key — the old raw key stops working immediately. Id, name, contact info, expiry and usage history
    /// are all kept; only the hash/prefix and <c>RolledOverAtUtc</c> change. Fails if the key doesn't exist
    /// or is already revoked (revoked keys can't be rolled over; create a new one instead).</summary>
    Task<RolloverApiKeyResponseDto> RolloverApiKeyAsync(RolloverApiKeyRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Validates a raw key from an incoming request against the configured master key first, then
    /// the stored, non-revoked, non-expired API keys. Called by <c>ApiKeyAuthenticationHandler</c> on every
    /// authenticated request.</summary>
    Task<ValidateApiKeyResponseDto> ValidateApiKeyAsync(ValidateApiKeyRequestDto request, CancellationToken cancellationToken = default);
}
