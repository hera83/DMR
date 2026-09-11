namespace DMR.Services.Identity.Dtos;

/// <summary>Response for <c>IIdentityService.RevokeApiKeyAsync</c>.</summary>
public class RevokeApiKeyResponseDto
{
    public bool Success { get; set; }

    /// <summary>Set when <see cref="Success"/> is false — e.g. no key with the given id exists.</summary>
    public string? ErrorMessage { get; set; }
}
