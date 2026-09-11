namespace DMR.Services.Identity.Dtos;

/// <summary>Response for <c>IIdentityService.ValidateApiKeyAsync</c>.</summary>
public class ValidateApiKeyResponseDto
{
    public bool IsValid { get; set; }

    /// <summary>Id of the <c>ApplicationUser</c> the request authenticates as (the master-key's synthetic
    /// identity, or a stored key's owning user). Set only when <see cref="IsValid"/> is true.</summary>
    public string? UserId { get; set; }

    /// <summary>Id of the matched <c>ApiKey</c> row. Null for a master-key match, since the master key is
    /// a config secret with no stored row.</summary>
    public string? KeyId { get; set; }

    /// <summary>The role to authenticate the request as — see <c>IdentityRoleNames</c>.</summary>
    public string? Role { get; set; }
}
