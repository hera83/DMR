namespace DMR.Services.Identity.Dtos;

/// <summary>
/// Response for <c>IIdentityService.RolloverApiKeyAsync</c>. Mirrors <see cref="RevokeApiKeyResponseDto"/>'s
/// success/error shape (the key id can be missing or already revoked) but additionally carries the new raw
/// key, since a successful rollover — unlike a revoke — hands back fresh secret material.
/// </summary>
public class RolloverApiKeyResponseDto
{
    public bool Success { get; set; }

    /// <summary>Set when <see cref="Success"/> is false — e.g. no key with the given id exists, or it's
    /// already revoked (a revoked key can't be rolled over; create a new one instead).</summary>
    public string? ErrorMessage { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? ContactInfo { get; set; }

    /// <summary>
    /// The new raw API key. Shown exactly once, same as <see cref="CreateApiKeyResponseDto.ApiKey"/> — the
    /// old raw key stops working the moment this is issued. Set only when <see cref="Success"/> is true.
    /// </summary>
    public string? ApiKey { get; set; }

    public DateTime? RolledOverAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
